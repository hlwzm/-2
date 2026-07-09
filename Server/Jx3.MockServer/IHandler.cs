namespace Jx3.MockServer;

public interface IHandler
{
    byte[]? Handle(uint msgId, uint seq, byte[] body);
}
