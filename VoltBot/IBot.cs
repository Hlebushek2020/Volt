using System;

namespace VoltBot;

public interface IBot
{
    DateTime StartDateTime { get; }
    void Shutdown(string reason = null);
}