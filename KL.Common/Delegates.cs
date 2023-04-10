namespace KL.Common;


public delegate Task AsyncEventHandler<in TEventArgs>(object? sender, TEventArgs e) where TEventArgs : EventArgs;

public delegate Task OnUpdate(string message);
