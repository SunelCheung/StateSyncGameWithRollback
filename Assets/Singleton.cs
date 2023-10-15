using System;

public class Singleton<T>
{
    protected static readonly T ms_instance = Activator.CreateInstance<T>();
    public static T Instance => ms_instance;

    protected Singleton() { }
}