using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.Core
{
    public static class GameplayEventBus
    {
        private static readonly List<Action<GameplayEvent>> Subscribers = new List<Action<GameplayEvent>>();

        public static int SubscriberCount => Subscribers.Count;

        public static void Subscribe(Action<GameplayEvent> handler)
        {
            if (handler == null || Subscribers.Contains(handler))
            {
                return;
            }

            Subscribers.Add(handler);
        }

        public static void Unsubscribe(Action<GameplayEvent> handler)
        {
            if (handler != null)
            {
                Subscribers.Remove(handler);
            }
        }

        public static void Publish(GameplayEvent gameplayEvent)
        {
            if (gameplayEvent.timestamp <= 0f)
            {
                gameplayEvent.timestamp = Time.unscaledTime;
            }

            for (int i = Subscribers.Count - 1; i >= 0; i--)
            {
                Subscribers[i]?.Invoke(gameplayEvent);
            }
        }

        public static void ClearForTests()
        {
            Subscribers.Clear();
        }
    }
}
