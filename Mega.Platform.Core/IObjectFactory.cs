namespace Mega.Platform.Core;

public interface IObjectFactory
{
    /// <summary>
    /// create a new one
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parameters"></param>
    /// <returns></returns>
    T CreateObject<T>(params object[] parameters);
    /// <summary>
    /// get an existing T if exists, otherwise create a new one
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parameters"></param>
    /// <returns></returns>
    T GetOrCreate<T>(params object[] parameters);
}
