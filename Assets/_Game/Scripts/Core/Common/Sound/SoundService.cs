using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace _Game.Scripts.Core.Common.Sound
{
    public class SoundService : ISoundService
    {
        public void PlayOneShotAt(EventReference eventRef, Vector3 position = default)
            => RuntimeManager.PlayOneShot(eventRef, position);

        public void PlayOneShot2D(EventReference eventRef)
            => RuntimeManager.PlayOneShot(eventRef);

        /// <summary>
        /// Метод, который запускает звук и возвращает его инстанс.
        /// Очищать звук самостоятельно!!! Иначе будет беда. Очищать можно через метод StopSound
        /// </summary>
        /// <param name="eventRef">Ссылка на сам звук</param>
        /// <param name="position">Необязательный параметр который нужен для 3д звука</param>
        /// <returns></returns>
        public EventInstance PlaySound(EventReference eventRef, Vector3 position = default)
        {
            var instance = RuntimeManager.CreateInstance(eventRef);

            if (position != default)
                instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));

            instance.start();

            return instance;
        }

        public void StopSound(EventInstance instance, STOP_MODE mode)
        {
            if (instance.isValid())
            {
                instance.stop(mode);
                instance.release();
            }
        }
    }
}