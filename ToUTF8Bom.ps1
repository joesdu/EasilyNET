# 定义要处理的文件夹路径
$folder = $pwd.Path
# 定义要转换的目标编码
$targetEncoding = "UTF8BOM"
# 定义要排除的文件夹名称
$exclude = "bin", "obj"
# 定义要转换的文件类型
$include = "\.cs|\.md|\.json|\.yml|\.sln|\.props"
# 获取文件夹及其子文件夹中的所有文件，排除指定的文件夹
$files = Get-ChildItem -Path $folder -Recurse -Exclude $exclude

# 遍历每个文件
foreach ($file in $files) {
    # 判断文件是否属于要转换的类型
    if ($file.Name -match $include) {
        # 获取文件的当前编码
        $currentEncoding = [System.Text.Encoding]::Default
        try {
            $currentEncoding = Get-FileEncoding -Path $file.FullName
        }
        catch {
            Write-Warning "无法获取文件 $file 的编码，使用默认编码。"
        }
        # 如果文件的编码与目标编码不同，则进行转换
        if ($currentEncoding -ne $targetEncoding) {
            # 读取文件内容
            $content = Get-Content -Path $file.FullName -Encoding $currentEncoding
            # 写入文件内容，使用目标编码
            Set-Content -Path $file.FullName -Encoding $targetEncoding -Value $content
            # 输出转换结果
            Write-Output "已将文件 $file 从 $currentEncoding 转换为 $targetEncoding 。"
        }
    }
}

# 定义一个函数，用于获取文件的编码
function Get-FileEncoding {
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $True, ValueFromPipelineByPropertyName = $True)]
        [string]$Path
    )

    [byte[]]$byte = Get-Content -Encoding byte -ReadCount 4 -TotalCount 4 -Path $Path
    if (!$byte) { return [Text.Encoding]::Default }

    switch ($byte[0..1] -join '-') {
        '254-255' { return [Text.Encoding]::BigEndianUnicode }
        '255-254' { return [Text.Encoding]::Unicode }
        '255-239' { return [Text.Encoding]::UTF8BOM }
        default {
            if ($byte[0] -eq 0) { return [Text.Encoding]::UTF32 }
            else { return [Text.Encoding]::Default }
        }
    }
}
