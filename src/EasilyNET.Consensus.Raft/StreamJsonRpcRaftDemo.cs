using EasilyNET.Consensus.Raft;

/// <summary>
/// StreamJsonRpc Raft 演示程序
/// </summary>
public class StreamJsonRpcRaftDemo
{
    /// <summary>
    /// 主入口点
    /// </summary>
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== StreamJsonRpc Raft 共识算法演示 ===");

        // 创建服务发现实例 (使用静态配置)
        var serviceDiscovery = new StaticServiceDiscovery(new Dictionary<string, NodeAddress>
        {
            ["node1"] = new NodeAddress("localhost", 5001),
            ["node2"] = new NodeAddress("localhost", 5002),
            ["node3"] = new NodeAddress("localhost", 5003)
        });

        // 创建集群
        var nodeIds = new List<string> { "node1", "node2", "node3" };
        var cluster = new StreamJsonRpcRaftCluster(nodeIds, serviceDiscovery);

        try
        {
            // 启动集群
            Console.WriteLine("启动 Raft 集群...");
            await cluster.StartAsync();

            // 等待集群稳定
            await Task.Delay(5000);

            // 显示集群状态
            DisplayClusterStatus(cluster);

            // 模拟客户端请求
            await SimulateClientRequestsAsync(cluster);

            // 等待用户输入
            Console.WriteLine("\n按任意键停止集群...");
            Console.ReadKey();

            // 停止集群
            Console.WriteLine("停止 Raft 集群...");
            await cluster.StopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"演示程序出错: {ex.Message}");
        }
        finally
        {
            cluster.Dispose();
        }
    }

    /// <summary>
    /// 显示集群状态
    /// </summary>
    private static void DisplayClusterStatus(StreamJsonRpcRaftCluster cluster)
    {
        Console.WriteLine("\n=== 集群状态 ===");
        var leader = cluster.GetLeader();
        if (leader != null)
        {
            // 通过反射获取 NodeId，因为 RaftNode 没有公开的 NodeId 属性
            var nodeIdProperty = leader.GetType().GetProperty("NodeId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nodeId = nodeIdProperty?.GetValue(leader)?.ToString() ?? "未知";
            Console.WriteLine($"领导者: {nodeId} (任期: {leader.CurrentTerm})");
        }
        else
        {
            Console.WriteLine("暂无领导者");
        }

        foreach (var node in cluster.GetNodes().Values)
        {
            var nodeIdProperty = node.GetType().GetProperty("NodeId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nodeId = nodeIdProperty?.GetValue(node)?.ToString() ?? "未知";
            Console.WriteLine($"节点 {nodeId}: {node.State} (任期: {node.CurrentTerm})");
        }
    }

    /// <summary>
    /// 模拟客户端请求
    /// </summary>
    private static async Task SimulateClientRequestsAsync(StreamJsonRpcRaftCluster cluster)
    {
        Console.WriteLine("\n=== 模拟客户端请求 ===");

        var leader = cluster.GetLeader();
        if (leader == null)
        {
            Console.WriteLine("没有找到领导者，无法提交请求");
            return;
        }

        // 提交一些测试命令
        var commands = new[]
        {
            "SET key1 value1",
            "SET key2 value2",
            "DELETE key1"
        };

        foreach (var command in commands)
        {
            try
            {
                // 使用 AppendLog 方法而不是直接创建 LogEntry
                var commandBytes = System.Text.Encoding.UTF8.GetBytes(command);
                var success = await leader.AppendLog(commandBytes);
                Console.WriteLine($"提交命令 '{command}': {(success ? "成功" : "失败")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"提交命令 '{command}' 失败: {ex.Message}");
            }

            await Task.Delay(1000); // 等待日志复制
        }
    }
}