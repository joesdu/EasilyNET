using EasilyNET.AutoDependencyInjection.Attributes;
using EasilyNET.AutoDependencyInjection.Modules;

// ReSharper disable ClassNeverInstantiated.Global

namespace WpfApp.Test;

/**
 * 要实现自动注入,一定要在这个地方添加,由于中间件的注册顺序会对程序产生巨大影响,因此请注意模块的注入顺序,服务配置的顺序无所谓.
 * 该处模块注入顺序为从上至下,本类AppWebModule最先注册.所以本类中中间件注册函数ApplicationInitialization最先执行.
 */
[DependsOn(typeof(DependencyAppModule))]
internal sealed class AppServiceModules : AppModule { }