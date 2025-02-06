using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using OpenNos.Core;

namespace OpenNos.GameObject.EventSubscriber
{
    public static class EventSubscriber
    {
        public static IDisposable SafeSubscribe(this IObservable<long> obs, Action<long> callback)
        {
            IDisposable observable = null;

            try
            {
                observable = obs.Subscribe(x =>
                {
                    try
                    {
                        callback(x);
                    }
                    catch
                    {
                        observable?.Dispose();
                    }
                });

                return observable;
            }
            catch (Exception e)
            {
                observable?.Dispose();
                Logger.Log.Error("SafeSubscribe Error :", e);
                return null;
            }
        }
    }

}
