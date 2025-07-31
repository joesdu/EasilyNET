//using EasilyNET.Ipc.Interfaces;
//using EasilyNET.Ipc.Models;

//namespace EasilyNET.Ipc.Serializers;

//public sealed class MessagePackIpcSerializer : IIpcSerializer
//{
//    public byte[] SerializeCommand(IpcCommand command) => MessagePackSerializer.Serialize(command);

//    public IpcCommand? DeserializeCommand(byte[] data) => MessagePackSerializer.Deserialize<IpcCommand>(data);

//    public byte[] SerializeResponse(IpcCommandResponse response) => MessagePackSerializer.Serialize(response);

//    public IpcCommandResponse? DeserializeResponse(byte[] data) => MessagePackSerializer.Deserialize<IpcCommandResponse>(data);
//}

