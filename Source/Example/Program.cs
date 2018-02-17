using System;
using System.Collections.Generic;
using System.Text;

using Cgen;
using Cgen.Audio;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            // This is ensure our SoundSystem will be disposed when program about to exit
            using (var system = SoundSystem.Instance)
            {
                // Hide Console Cursor
                Console.CursorVisible = false;

                // Now with music
                var music = new Music("./Resources/orchestral.ogg");
                system.Play(music);

                var length = music.Duration;
                Console.WriteLine("Playing: ochestral");
                Console.WriteLine("Implementation: Music");
                while (music.Status == SoundStatus.Playing)
                {
                    system.Update();
                    var offset = music.PlayingOffset;
                    Console.Write("\r{0}:{1}:{2} - {3}:{4}:{5}",
                        offset.Minutes.ToString("00"), offset.Seconds.ToString("00"), offset.Milliseconds.ToString("000"),
                        length.Minutes.ToString("00"), length.Seconds.ToString("00"), length.Milliseconds.ToString("000")
                    );
                }

                // Load and play the audio with Sound class
                var sound = new Sound(new SoundBuffer("./Resources/canary.wav"));
                system.Play(sound);

                length = sound.Duration;
                Console.WriteLine("\n\nPlaying: canary.wav");
                Console.WriteLine("Implementation: Sound");
                while (sound.Status == SoundStatus.Playing)
                {
                    system.Update();
                    var offset = sound.PlayingOffset;
                    Console.Write("\r{0}:{1}:{2} - {3}:{4}:{5}",
                        offset.Minutes.ToString("00"), offset.Seconds.ToString("00"), offset.Milliseconds.ToString("000"),
                        length.Minutes.ToString("00"), length.Seconds.ToString("00"), length.Milliseconds.ToString("000")
                    );
                }

                Console.WriteLine("\n\nPress any key to exit program.");
                Console.ReadKey(true);
            }
        }
    }
}
