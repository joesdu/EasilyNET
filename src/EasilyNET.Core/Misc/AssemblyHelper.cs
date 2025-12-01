using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Enumeration;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Assembly helper class</para>
///     <para xml:lang="zh">程序集帮助类</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Overview:</para>
///     <para xml:lang="zh">用法概览:</para>
///     <list type="number">
///         <item>
///             <description>
///                 <para xml:lang="en">Fast assembly discovery with include/exclude wildcard patterns (*, ?) and caching.</para>
///                 <para xml:lang="zh">通过包含/排除通配符(*, ?)与缓存实现快速装配发现.</para>
///             </description>
///         </item>
///         <item>
///             <description>
///                 <para xml:lang="en">Prefer already loaded assemblies and DependencyContext, optionally probe disk.</para>
///                 <para xml:lang="zh">优先使用已加载装配与 DependencyContext,可选磁盘探测.</para>
///             </description>
///         </item>
///         <item>
///             <description>
///                 <para xml:lang="en">Resilient type loading (handles ReflectionTypeLoadException).</para>
///                 <para xml:lang="zh">类型加载具备容错(处理 ReflectionTypeLoadException).</para>
///             </description>
///         </item>
///         <item>
///             <description>
///                 <para xml:lang="en">Cached queries for attributes and assignable types.</para>
///                 <para xml:lang="zh">针对"特性查询/可赋值类型查询"提供结果缓存.</para>
///             </description>
///         </item>
///         <item>
///             <description>
///                 <para xml:lang="en">Configurable via Configure and helper methods (AddIncludePatterns / AddExcludePatterns).</para>
///                 <para xml:lang="zh">通过 Configure 及 AddIncludePatterns / AddExcludePatterns 等方法进行配置.</para>
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// <![CDATA[
/// // 1) 默认：全量扫描（默认排除 System.* / Microsoft.* 等），直接取结果
/// var assemblies = AssemblyHelper.AllAssemblies;
/// var types = AssemblyHelper.AllTypes;
/// 
/// // 2) 推荐：限制扫描范围，提高速度（关闭全量扫描与磁盘探测，添加自定义包含前缀）
/// AssemblyHelper.Configure(o =>
/// {
///     o.ScanAllRuntimeLibraries = false;
///     o.AllowDirectoryProbe = false;
/// });
/// AssemblyHelper.AddIncludePatterns("EasilyNET.*", "MyCompany.*");
/// 
/// // 3) 查找具有指定特性的类型（如在 Swagger 中按特性分组）
/// var grouped = AssemblyHelper.FindTypesByAttribute<ApiGroupAttribute>();
/// 
/// // 4) 查找可赋值类型（接口/基类的实现）
/// var handlers = AssemblyHelper.FindTypesAssignableTo<IMyHandler>();
/// 
/// // 5) 按名称获取程序集（支持通配符）
/// var pluginAssemblies = AssemblyHelper.GetAssembliesByName(new[] { "Plugin.*" });
/// 
/// // 6) 修改配置后，如需强制重新计算，清理缓存
/// AssemblyHelper.ClearCaches();
/// ]]>
/// </code>
/// </example>
public static class AssemblyHelper
{
    // Cache: assembly full name -> Assembly (or null when failed to load)
    private static readonly ConcurrentDictionary<string, Assembly?> AssemblyCache = new();

    // Attribute results cache: (attrType, inherit, version) -> types
    private static readonly ConcurrentDictionary<(Type attrType, bool inherit, int version), Lazy<IReadOnlyList<Type>>> AttributeTypeCache = new();

    // Lazily computed snapshots. We recreate these when options change.
    private static Lazy<Assembly[]> _lazyAllAssemblies = new(static () =>
    {
        ArgumentNullException.ThrowIfNull(Options);
        return [.. LoadAssembliesInternal(Options)];
    });

    private static Lazy<Type[]> _lazyAllTypes = new(static () => LoadTypesInternal(_lazyAllAssemblies.Value));

    // Bump when options/caches are reset to invalidate attr caches
    private static volatile int _version;

    // Options to control scanning behavior
    private static readonly Lock _optionsLock = new();

    private static readonly ParallelOptions parallelOptions = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    static AssemblyHelper()
    {
        // Initialize default include patterns
        var entryAssembly = Assembly.GetEntryAssembly();
        var entryAssemblyName = entryAssembly?.GetName().Name;
        if (!entryAssemblyName.IsNotNullOrWhiteSpace())
        {
            return;
        }
        Options.IncludePatterns.Add(entryAssemblyName);
        var prefix = entryAssemblyName.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            Options.IncludePatterns.Add($"{prefix}.*");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets all assemblies that match the criteria</para>
    ///     <para xml:lang="zh">获取所有符合条件的程序集</para>
    /// </summary>
    public static IEnumerable<Assembly> AllAssemblies => _lazyAllAssemblies.Value;

    /// <summary>
    ///     <para xml:lang="en">Gets all types from the assemblies that match the criteria</para>
    ///     <para xml:lang="zh">从符合条件的程序集获取所有类型</para>
    /// </summary>
    public static IEnumerable<Type> AllTypes => _lazyAllTypes.Value;

    // Options holder and helpers
    /// <summary>
    /// Options for assembly scanning.
    /// </summary>
    private static AssemblyScanOptions Options { get; } = new();

    /// <summary>
    ///     <para xml:lang="en">
    ///     Adds assembly names to be loaded when <see cref="AssemblyScanOptions.ScanAllRuntimeLibraries" /> is set to
    ///     <see langword="false" />.
    ///     </para>
    ///     <para xml:lang="zh">当 <see cref="AssemblyScanOptions.ScanAllRuntimeLibraries" /> 设置为 <see langword="false" /> 时，添加要加载的程序集名称。</para>
    /// </summary>
    /// <param name="names">
    ///     <para xml:lang="en">The names of the assemblies to add.</para>
    ///     <para xml:lang="zh">要添加的程序集名称。</para>
    /// </param>
    public static void AddAssemblyNames(params IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var added = false;
        foreach (var n in names)
        {
            if (string.IsNullOrWhiteSpace(n))
            {
                continue;
            }
            if (Options.IncludePatterns.Add(n))
            {
                added = true;
            }
        }
        if (!added)
        {
            return;
        }
        Options.InvalidatePatternCache();
        Reset();
    }

    /// <summary>
    ///     <para xml:lang="en">Configures assembly scan options</para>
    ///     <para xml:lang="zh">配置程序集扫描选项</para>
    /// </summary>
    public static void Configure(Action<AssemblyScanOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        lock (_optionsLock)
        {
            configure(Options);
            Reset();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Clears internal caches and recomputes assemblies/types on next access</para>
    ///     <para xml:lang="zh">清除内部缓存并在下次访问时重新计算程序集/类型</para>
    /// </summary>
    public static void ClearCaches() => Reset();

    /// <summary>
    ///     <para xml:lang="en">Adds include patterns</para>
    ///     <para xml:lang="zh">添加包含模式</para>
    /// </summary>
    public static void AddIncludePatterns(params string[]? patterns)
    {
        if (patterns is null || patterns.Length == 0)
        {
            return;
        }
        var added = false;
        foreach (var p in patterns)
        {
            if (string.IsNullOrWhiteSpace(p))
            {
                continue;
            }
            if (Options.IncludePatterns.Add(p))
            {
                added = true;
            }
        }
        if (!added)
        {
            return;
        }
        Options.InvalidatePatternCache();
        Reset();
    }

    /// <summary>
    ///     <para xml:lang="en">Adds exclude patterns</para>
    ///     <para xml:lang="zh">添加排除模式</para>
    /// </summary>
    public static void AddExcludePatterns(params string[]? patterns)
    {
        if (patterns is null || patterns.Length == 0)
        {
            return;
        }
        var added = false;
        foreach (var p in patterns)
        {
            if (string.IsNullOrWhiteSpace(p))
            {
                continue;
            }
            if (Options.ExcludePatterns.Add(p))
            {
                added = true;
            }
        }
        if (!added)
        {
            return;
        }
        Options.InvalidatePatternCache();
        Reset();
    }

    /// <summary>
    ///     <para xml:lang="en">Gets assemblies by their names</para>
    ///     <para xml:lang="zh">通过名称获取程序集</para>
    /// </summary>
    /// <param name="assemblyNames">
    ///     <para xml:lang="en">Assembly name</para>
    ///     <para xml:lang="zh">程序集名称,支持通配符匹配,如: EasilyNET* </para>
    /// </param>
    [RequiresUnreferencedCode("This method uses reflection and may not be compatible with AOT.")]
    public static IEnumerable<Assembly?> GetAssembliesByName(params IEnumerable<string> assemblyNames)
    {
        var patterns = CompileWildcardPatterns(assemblyNames);
        if (patterns.Count == 0)
        {
            yield break;
        }
        var yieldedFullNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Prefer already loaded assemblies
        foreach (var asm in AssemblyLoadContext.Default.Assemblies)
        {
            var name = asm.GetName().Name ?? string.Empty;
            if (!MatchesAnyPattern(name, patterns))
            {
                continue;
            }
            var fullName = asm.FullName;
            if (fullName is not null && yieldedFullNames.Add(fullName))
            {
                yield return asm;
            }
        }
        // Use DependencyContext for known assemblies in the app
        var dcNames = DependencyContext.Default?.GetDefaultAssemblyNames() ?? [];
        foreach (var name in dcNames)
        {
            var simpleName = name.Name ?? string.Empty;
            if (!MatchesAnyPattern(simpleName, patterns))
            {
                continue;
            }
            if (!TryLoadByName(name, out var asm) || asm is null)
            {
                continue;
            }
            var fullName = asm.FullName;
            if (fullName is not null && yieldedFullNames.Add(fullName))
            {
                yield return asm;
            }
        }
        // As a last resort, probe disk if allowed
        if (!Options.AllowDirectoryProbe)
        {
            yield break;
        }
        foreach (var asm in ProbeAssembliesFromDisk(patterns))
        {
            var fullName = asm.FullName;
            if (fullName is not null && yieldedFullNames.Add(fullName))
            {
                yield return asm;
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Finds types that match the specified predicate</para>
    ///     <para xml:lang="zh">查找符合指定谓词的类型</para>
    /// </summary>
    /// <param name="predicate">
    ///     <para xml:lang="en">The predicate to match types</para>
    ///     <para xml:lang="zh">匹配类型的谓词</para>
    /// </param>
    public static IEnumerable<Type> FindTypes(Func<Type, bool> predicate) => AllTypes.Where(predicate);

    /// <summary>
    ///     <para xml:lang="en">Finds all types marked with the specified attribute</para>
    ///     <para xml:lang="zh">查找所有标有指定属性的类型</para>
    /// </summary>
    public static IEnumerable<Type> FindTypesByAttribute<TAttribute>(bool inherit = true) where TAttribute : Attribute => FindTypesByAttribute(typeof(TAttribute), inherit);

    /// <summary>
    ///     <para xml:lang="en">Finds all types marked with the specified attribute</para>
    ///     <para xml:lang="zh">查找所有标有指定属性的类型</para>
    /// </summary>
    public static IEnumerable<Type> FindTypesByAttribute<TAttribute>(Func<Type, bool> predicate, bool inherit = true) where TAttribute : Attribute => FindTypesByAttribute<TAttribute>(inherit).Where(predicate);

    /// <summary>
    ///     <para xml:lang="en">Finds all types marked with the specified attribute</para>
    ///     <para xml:lang="zh">查找所有标有指定属性的类型</para>
    /// </summary>
    public static IEnumerable<Type> FindTypesByAttribute(Type type, bool inherit = true)
    {
        var key = (type, inherit, _version);
        var cached = AttributeTypeCache.GetOrAdd(key, k =>
            new(() => [.. AllTypes.Where(a => SafeIsDefined(a, k.attrType, k.inherit)).Distinct()], true));
        return cached.Value;
    }

    /// <summary>
    ///     <para xml:lang="en">Finds types assignable to TBase (classes or interfaces)</para>
    ///     <para xml:lang="zh">查找可分配给 TBase 的类型（类或接口）</para>
    /// </summary>
    public static IEnumerable<Type> FindTypesAssignableTo<TBase>()
    {
        var baseType = typeof(TBase);
        return AllTypes.Where(t => baseType.IsAssignableFrom(t) && t != baseType);
    }

    /// <summary>
    ///     <para xml:lang="en">Finds assemblies that match the specified predicate</para>
    ///     <para xml:lang="zh">查找符合指定谓词的程序集</para>
    /// </summary>
    /// <param name="predicate">
    ///     <para xml:lang="en">The predicate to match assemblies</para>
    ///     <para xml:lang="zh">匹配程序集的谓词</para>
    /// </param>
    public static IEnumerable<Assembly> FindAllItems(Func<Assembly, bool> predicate) => AllAssemblies.Where(predicate);

    private static Type[] LoadTypesInternal(IEnumerable<Assembly> assemblies)
    {
        var asmArray = assemblies as Assembly[] ?? [.. assemblies];
        if (asmArray.Length == 0)
        {
            return [];
        }
        // Pre-collect types per assembly to reduce contention
        var typeArrays = new Type[asmArray.Length][];
        Parallel.For(0, asmArray.Length, parallelOptions, i =>
        {
            var assembly = asmArray[i];
            try
            {
                typeArrays[i] = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                // Filter out null types - count without creating iterator
                var loadedTypes = rtle.Types;
                var count = loadedTypes.OfType<Type>().Count();
                var filtered = new Type[count];
                var idx = 0;
                foreach (var t in loadedTypes)
                {
                    if (t is not null)
                    {
                        filtered[idx++] = t;
                    }
                }
                typeArrays[i] = filtered;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load types from assembly: {assembly.FullName}, error: {ex.Message}");
                typeArrays[i] = [];
            }
        });
        // Calculate total count and merge
        var totalCount = typeArrays.Sum(arr => arr.Length);
        var result = new Type[totalCount];
        var offset = 0;
        foreach (var arr in typeArrays)
        {
            if (arr.Length == 0)
            {
                continue;
            }
            Array.Copy(arr, 0, result, offset, arr.Length);
            offset += arr.Length;
        }
        return result;
    }

    /// <summary>
    ///     <para xml:lang="en">Load assemblies according to the provided options (loaded, DependencyContext, optional disk probing).</para>
    ///     <para xml:lang="zh">根据提供的选项加载程序集（已加载程序集、DependencyContext、可选磁盘探测）</para>
    /// </summary>
    private static IEnumerable<Assembly> LoadAssembliesInternal(AssemblyScanOptions options)
    {
        var result = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        // 1) Already loaded assemblies
        foreach (var asm in AssemblyLoadContext.Default.Assemblies)
        {
            if (!MatchAssembly(asm.GetName(), options))
            {
                continue;
            }
            if (asm.FullName.IsNotNullOrWhiteSpace())
            {
                _ = result.TryAdd(asm.FullName, asm);
            }
        }

        // 2) Assemblies from DependencyContext (fast path) - DependencyContext scanning now happens unconditionally, always consider DependencyContext and filter via include/exclude
        IEnumerable<AssemblyName> candidateNames = [];
        try
        {
            var dc = DependencyContext.Default;
            if (dc is not null)
            {
                candidateNames = dc.GetDefaultAssemblyNames();
            }
        }
        catch
        {
            // ignore
        }
        var filteredNames = candidateNames.Where(name => MatchAssembly(name, options)).ToArray();
        Parallel.ForEach(filteredNames, parallelOptions, name =>
        {
            if (!TryLoadByName(name, out var asm))
            {
                return;
            }
            if (asm is not null && asm.FullName.IsNotNullOrWhiteSpace())
            {
                result.TryAdd(asm.FullName, asm);
            }
        });

        // 3) Optionally probe disk
        if (!options.AllowDirectoryProbe)
        {
            return result.Values;
        }
        var patterns = CompileWildcardPatterns(options.IncludePatterns);
        foreach (var asm in ProbeAssembliesFromDisk(patterns))
        {
            if (asm.FullName.IsNotNullOrWhiteSpace())
            {
                result.TryAdd(asm.FullName, asm);
            }
        }
        return result.Values;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Assembly))]
    private static bool TryLoadByName(AssemblyName assemblyName, out Assembly? asm)
    {
        asm = null;
        if (AssemblyCache.TryGetValue(assemblyName.FullName, out var cached))
        {
            if (cached is null)
            {
                return false;
            }
            asm = cached;
            return true;
        }
        try
        {
            var loaded = Assembly.Load(assemblyName);
            AssemblyCache[assemblyName.FullName] = loaded;
            asm = loaded;
            return true;
        }
        catch (Exception ex)
        {
            AssemblyCache[assemblyName.FullName] = null;
            Debug.WriteLine($"Failed to load assembly: {assemblyName.Name}, error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Check whether the assembly name matches include/exclude patterns under the given options.</para>
    ///     <para xml:lang="zh">判断程序集名称是否在给定选项下匹配包含/排除模式。</para>
    /// </summary>
    private static bool MatchAssembly(AssemblyName name, AssemblyScanOptions options)
    {
        var simple = name.Name ?? string.Empty;
        if (options.IncludePatterns.Count == 0 && options.ExcludePatterns.Count == 0)
        {
            return options.ScanAllRuntimeLibraries;
        }
        // Exclude first - use foreach to avoid LINQ overhead
        var excludePatterns = options.CompiledExcludePatterns;
        if (excludePatterns.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, simple)))
        {
            return false;
        }
        // Then include
        if (options.IncludePatterns.Count == 0)
        {
            return true;
        }
        var includePatterns = options.CompiledIncludePatterns;
        return includePatterns.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, simple));
    }

    /// <summary>
    ///     <para xml:lang="en">Check if name matches any pattern in the set (avoids LINQ delegate overhead).</para>
    ///     <para xml:lang="zh">检查名称是否匹配集合中的任何模式（避免 LINQ 委托开销）。</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchesAnyPattern(string name, FrozenSet<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (FileSystemName.MatchesSimpleExpression(pattern, name))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Compile wildcard patterns (supports '*' and '?') into a frozen set of pattern strings for MatchesSimpleExpression.</para>
    ///     <para xml:lang="zh">将通配符模式（支持 "*" 与 "?"）编译为用于 MatchesSimpleExpression 的不可变字符串集合。</para>
    /// </summary>
    private static FrozenSet<string> CompileWildcardPatterns(IEnumerable<string> patterns)
    {
        var list = (from raw in patterns
                    let trimmed = raw?.Trim()
                    where !string.IsNullOrWhiteSpace(trimmed)
                    select trimmed!).ToList();
        return list.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     <para xml:lang="en">Probe application directory for assemblies and load those whose names match provided patterns.</para>
    ///     <para xml:lang="zh">在应用目录中探测程序集并加载与给定模式匹配的程序集。</para>
    /// </summary>
    private static IEnumerable<Assembly> ProbeAssembliesFromDisk(FrozenSet<string> patterns)
    {
        IEnumerable<string> files = [];
        try
        {
            files = Directory.GetFiles(AppContext.BaseDirectory, "*.dll", SearchOption.AllDirectories);
        }
        catch
        {
            // ignore
        }
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!MatchesAnyPattern(fileName, patterns))
            {
                continue;
            }
            AssemblyName? asmName;
            try
            {
                asmName = AssemblyLoadContext.GetAssemblyName(file);
            }
            catch
            {
                continue;
            }
            if (AssemblyCache.TryGetValue(asmName.FullName, out var cached) && cached is not null)
            {
                yield return cached;
                continue;
            }
            Assembly? loaded = null;
            try
            {
                loaded = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                AssemblyCache[asmName.FullName] = loaded;
            }
            catch (Exception ex)
            {
                AssemblyCache[asmName.FullName] = null;
                Debug.WriteLine($"Failed to load assembly from path: {file}, error: {ex.Message}");
            }
            if (loaded is not null)
            {
                yield return loaded;
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Safely check whether a type is decorated with the specified attribute, swallowing reflection errors.</para>
    ///     <para xml:lang="zh">安全地检查类型是否带有指定特性，避免反射异常导致失败。</para>
    /// </summary>
    private static bool SafeIsDefined(Type t, Type attr, bool inherit)
    {
        try
        {
            return t.IsDefined(attr, inherit);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Reset internal caches and snapshots, forcing recomputation on next access.</para>
    ///     <para xml:lang="zh">重置内部缓存与快照，在下次访问时强制重新计算。</para>
    /// </summary>
    private static void Reset()
    {
        Interlocked.Increment(ref _version);
        AttributeTypeCache.Clear();
        _lazyAllAssemblies = new(static () => [.. LoadAssembliesInternal(Options)]);
        _lazyAllTypes = new(static () => [.. LoadTypesInternal(_lazyAllAssemblies.Value)]);
    }

    /// <summary>
    ///     <para xml:lang="en">Scanning options</para>
    ///     <para xml:lang="zh">扫描选项</para>
    /// </summary>
    public sealed class AssemblyScanOptions
    {
        private readonly Lock _patternLock = new();
        private volatile FrozenSet<string>? _compiledExcludePatterns;
        private volatile FrozenSet<string>? _compiledIncludePatterns;
        private int _lastExcludeVersion = -1;
        private int _lastIncludeVersion = -1;
        private volatile int _patternVersion;

        internal AssemblyScanOptions()
        {
            // Reasonable defaults
            ScanAllRuntimeLibraries = true;
            AllowDirectoryProbe = true;
            // Exclude common framework assemblies by default to improve speed
            ExcludePatterns = new(StringComparer.OrdinalIgnoreCase)
            {
                "System.*",
                "Microsoft.*",
                "netstandard",
                "mscorlib",
                "WindowsBase",
                "Newtonsoft.*",
                "Serilog.*",
                "NLog.*",
                "Npgsql.*",
                "Oracle.*",
                "MySql.*",
                "SQLite.*"
            };
            IncludePatterns = new(StringComparer.OrdinalIgnoreCase)
            {
                "EasilyNET*"
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Scan all runtime libraries reported by DependencyContext</para>
        ///     <para xml:lang="zh">扫描 DependencyContext 报告的所有运行时程序集</para>
        /// </summary>
        public bool ScanAllRuntimeLibraries { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Allow probing the app directory for additional DLLs when not found</para>
        ///     <para xml:lang="zh">允许在未找到时从应用程序目录探测 DLL</para>
        /// </summary>
        public bool AllowDirectoryProbe { get; set; }

        internal FrozenSet<string> CompiledIncludePatterns
        {
            get
            {
                var currentVersion = _patternVersion;
                var cached = _compiledIncludePatterns;
                if (cached is not null && Volatile.Read(ref _lastIncludeVersion) == currentVersion)
                {
                    return cached;
                }
                lock (_patternLock)
                {
                    currentVersion = _patternVersion;
                    if (_compiledIncludePatterns is not null && _lastIncludeVersion == currentVersion)
                    {
                        return _compiledIncludePatterns;
                    }
                    _compiledIncludePatterns = CompileWildcardPatterns(IncludePatterns);
                    Volatile.Write(ref _lastIncludeVersion, currentVersion);
                    return _compiledIncludePatterns;
                }
            }
        }

        internal FrozenSet<string> CompiledExcludePatterns
        {
            get
            {
                var currentVersion = _patternVersion;
                var cached = _compiledExcludePatterns;
                if (cached is not null && Volatile.Read(ref _lastExcludeVersion) == currentVersion)
                {
                    return cached;
                }
                lock (_patternLock)
                {
                    currentVersion = _patternVersion;
                    if (_compiledExcludePatterns is not null && _lastExcludeVersion == currentVersion)
                    {
                        return _compiledExcludePatterns;
                    }
                    _compiledExcludePatterns = CompileWildcardPatterns(ExcludePatterns);
                    Volatile.Write(ref _lastExcludeVersion, currentVersion);
                    return _compiledExcludePatterns;
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Include patterns, e.g. "EasilyNET.*"</para>
        ///     <para xml:lang="zh">包含模式，例如 "EasilyNET.*"</para>
        /// </summary>
        public HashSet<string> IncludePatterns { get; }

        /// <summary>
        ///     <para xml:lang="en">Exclude patterns, e.g. "System.*"</para>
        ///     <para xml:lang="zh">排除模式，例如 "System.*"</para>
        /// </summary>
        public HashSet<string> ExcludePatterns { get; }

        /// <summary>
        ///     <para xml:lang="en">Invalidates compiled pattern caches</para>
        ///     <para xml:lang="zh">使编译的模式缓存失效</para>
        /// </summary>
        internal void InvalidatePatternCache() => Interlocked.Increment(ref _patternVersion);
    }
}