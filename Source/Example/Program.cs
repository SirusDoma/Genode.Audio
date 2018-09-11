using System;
using System.Collections.Generic;
using System.Text;

using Genode;
using Genode.Audio;

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
                var music = system.LoadSound("./Resources/orchestral.ogg", BufferMode.Stream);
                var musicChannel = system.Play(music);

                var length = music.Duration;
                Console.WriteLine("Playing: ochestral");
                Console.WriteLine("Implementation: Music");
                while (musicChannel.Status == SoundStatus.Playing)
                {
                    system.Update(0D);
                    
                    var offset = musicChannel.PlayingOffset;
                    Console.Write($"\r{offset:mm\\:ss\\:ff} - {length:mm\\:ss\\:ff}");
                }
                
                // Load and play the audio with Sound class
                var sound = system.LoadSound("./Resources/canary.wav", BufferMode.Sample);
                var soundChannel = system.Play(sound);

                length = sound.Duration;
                Console.WriteLine("\n\nPlaying: canary.wav");
                Console.WriteLine("Implementation: Sound");
                while (soundChannel.Status == SoundStatus.Playing)
                {
                    system.Update(0D);
                    
                    var offset = soundChannel.PlayingOffset;
                    Console.Write($"\r{offset:mm\\:ss\\:ff} - {length:mm\\:ss\\:ff}");
                }

                Console.WriteLine("\n\nPress any key to exit program.");
                Console.ReadKey(true);
            }
        }
    }
}
