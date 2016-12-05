# Cgen.Audio #
Audio Submodule of CygnusJam Game Engine.

- **Author**: Alghi Fariansyah
- **Email**: com@cxo2.me
- **Latest Version**: 1.0

## Summary ##

Cgen.Audio is a simple yet powerful Cross-platform Audio Engine which is provides audio playbacks.
Written under C# language based on [NVorbis](https://github.com/SirusDoma/nvorbis) and [OpenTK](https://github.com/opentk/opentk) (OpenAL).  

The main idea is to provide an audio playback that simple and fast. If you prefer simplicity over fancy features, then this audio engine is for you.

## Compiling Project ##

By default, the project target framework is targeted to .NET 2.0 to ensure maximum backward compatibility against old hardware and / or old projects targeted to old framework. This framework is incompatible with .NET Core since it OpenTK that has not officially supported .NET Core.  

It is required to configure the Build Configuration Platform (`x86`/`x64`) of target application to match the library configuration. Avoid using `Any CPU` platform, because this framework uses native external dependencies (e.g: the engine may fail when deciding which version of `openal32.dll` to use).  

## Documentation ##

Currently, the engine doesn't have detailed API reference.
Therefore, the basic usages is covered along with this document.

Before getting started with the engine, you need to choose the proper usages of the engine, it depends on application that you are developing.

### 1. Initialization ###

The `SoundSystem` engine is actually initialized on the fly, you don't even have to call any method to initialize or choosing which audio device that you should pick or configuring specific hardware dependent configuration.
The engine will setup everything automagically.  

In case you want to initialize the `SoundSystem` explicitly, call `SoundSystem.Instance.Initialize()`

### 2. Handling Update Cycle ###

As described in Usages Notes, the `SoundSystem` need to updated frequently in order to stream the sound buffer and manage sound source lifecycle properly.  

There are 2 ways to Update the `SoundSystem`
- In a General Application (e.g: Winforms or WPF), it is recommended to use Automated Update Cycle by calling `SoundSystem.Instance.AutoUpdate();` at initialization of program. This will create a separate thread that automatically call `SoundSystem.Instance.Update()` each 10ms.  

- In a Game Application, call `SoundSystem.Instance.Update()` on each frame in the game loop.

### 3. Creating a Sound / Music object ###

Either `Sound` or `Music`, both inherit `SoundSource` elements, which provides basic audio properties and behaviors across audio implementation. In other words, you can implement your own Audio implementation by inherit `SoundSource` abstract class.

There is no maximum instances for the `SoundSource`, you can create `Sound` or `Music` objects as much as you like. However, you can only play up to 255 `SoundSource` at the same time. This is more than enough to simulate an orchestra.

`Sound` is a lightweight object that plays loaded audio data from a `SoundBuffer`. It should be used for small sounds that can fit in memory and should suffer no lag when they are played.  

For example are sound effect like door bells, or game effect such as gun shot, footstep, etc.
In order to create `Sound`, `SoundBuffer` is required, you can construct `SoundBuffer` from file, `Stream` or even an array of bytes

```
    // Create a sound object
    Sound bell = new Sound(new SoundBuffer("Path/To/Bell.wav"));

    // Or a System.IO.Stream
    System.IO.FileStream fstream = System.IO.File.OpenRead("Path/To/Bell.wav");
    Sound bell = new Sound(new SoundBuffer(fstream));

    // You can also load it from array of byte
    byte[] audioData = System.IO.File.ReadAllBytes("Path/To/Bell,wav");
    Sound bell = new Sound(new SoundBuffer(audioData));
```

`Music` doesn't load all the audio data into memory, instead it streams it on the fly from the source file. It is typically used to play compressed music that lasts several minutes, and would otherwise take many seconds to load and take hundreds of MB in memory due large amount of decoded samples.

Constructing `Music` object is similar to constructing `Sound` object, the main difference is you don't have to create `SoundBuffer` because `Music` will stream instead load whole samples into memory.

```
    // Create a music object
    Music bgm = new Music("Path/To/bgm.wav");

    // Or a System.IO.Stream
    System.IO.FileStream fstream = System.IO.File.OpenRead("Path/To/bgm.wav");
    Music bgm = new Music(fstream);

    // You can also load it from array of byte
    byte[] audioData = System.IO.File.ReadAllBytes("Path/To/bgm,wav");
    Music bgm = new Music(audioData);
```

### 4. Performing Playback Operations ###

The `Sound` and `Music` provide more or less the same features, You can `Play`, `Stop` and `Pause` a `Sound` or `Music` object at any point of your program, use `Play` to resume the paused `SoundSource` instance.

```
    // Play the sound
    SoundSystem.Instance.Play(bell);

    // Pause the sound
    SoundSystem.Instance.Pause(bell);

    // Stop the sound
    SoundSystem.Instance.Stop(bell);

    // You can also retrieve the status of sound
    Console.WriteLine(bell.Status); // SoundStatus.Stopped

    // Once the sound is no longer needed, dispose it
    bell.Dispose();
```

### 5. Modifying Audio Elements ###

And of course you can configure sound elements such as `Volume`, `Pitch`, `IsLooping` etc
It also contains some 3D Sounds Settings such as `Position`, `Attenuation` and `MinDistance`

```
    // 50% Volume
    bell.Volume = 50f;

    // 200% Pitch
    bell.Pitch = 2.0f;

    // Position of Sound will stay remain at same position of listener in 3D plane
    bell.IsRelativeToListener = false;

    // Set the Minimum Distance: the maximum distance at which it is heard at its maximum volume.
    bell.MinDistance = 3f;

    // Set an attenuation factor: a multiplicative factor which makes the sound more or less loud according to its distance from the listener.
    bell.Attenuation = 5f;

    // Set the 3D Position of the sound in 3D Plane.
    bell.Position = new Cgen.Vector3(1, 2, 3);

    // The sound will loop.
    bell.IsLooping = true;

    // Seek to 0:34
    bell.PlayingOffset = TimeSpan.FromSeconds(34);

     // Retrieve Sound Length, in Seconds
    Console.WriteLine(bell.Duration.TotalSeconds);
```

3D effect also can be achieved using `Listener` class that act as a single actor, which mean all instances of `SoundSource` will affected.

### 6. Using SoundGroup ###

`SoundGroup` provides a convenient way to manipulate a sets of `SoundSource` properties. For example, you want to set specific value of volume across multiple `Sound` or `Music` instances:

```
    // Initializes new instances of Sound and Music class
    Sound shot = ...
    Music bgm = ...

    // Initializes a new instance of SoundGroup
    SoundGroup group = new SoundGroup(shot, bgm);

    // Set the volume for every SoundSource in the group
    group.SetVolume(50f);

    // Play the SoundGroup
    SoundSystem.Instance.Play(group);
```

Note that `SoundGroup` uses method instead property to set the audio elements. `SoundGroup` still allow the instance to modify its own audio elements outside from the group, therefore, the `SoundGroup` does not provides a way to detect the audio element since each of them may vary from the other `SoundSource` in the group.

### 7. Cleanup ###

It is recommended to perform the cleanup before exiting the application or the library functionality is no longer required.  

```
    // Dispose SoundSystem
    SoundSystem.Instance.Dispose();
```

Note this will stop all playing sounds and music instances and dispose them. Additionally, it will stop the automatic update cycle if it was requested before.  

Performing any playback operation or modifying audio properties is no longer safe after this point.

## Extending SoundSystem ##

It is possible to extend the `SoundSystem` to perform customized code. In case you want to perform OpenAL specific codes, you need to provide your own or use existing OpenAL library. This audio engine uses [OpenTK](https://github.com/opentk/opentk) to perform OpenAL operations, however it is completely up to you to choose OpenAL implementation.  

For example, you want to create your own OpenAL `Context` manually by yourself, Cgen.Audio doesn't provides such functionality, but you can implement your own:

```
    public class CustomSoundSystem : Cgen.Audio.SoundSystem
    {
        // Actually, it is not required to use singleton instances
        // Hoever, it will make things simpler

        // Make sure you pass the OpenAL Context to the base Constructor
        // This will tell the SoundSystem to use OpenAL context that provided by you
        public CustomSoundSystem()
            : base(CreateContext())
        {
        }

        private static IntPtr CreateContext()
        {
            // Create your OpenAL Context here
        }

        public override void Dispose()
        {
            // Dispose your context here
            base.Dispose();
        }
    }
```

You can also call specific OpenAL function between playback functions (`Play()`, `Pause()` and `Stop()`). Unlike OpenGL, OpenAL may not contains specific states, however it is recommended to check for OpenAL error when performing OpenAL operations.  

Cgen.Audio provides OpenAL Error Checker. It is part of internal module, but it exposed to public and you can use it.

```
    using Cgen.Internal.OpenAL;

    public class CustomSoundSystem : Cgen.Audio.SoundSystem
    {
        // ...

        public override Play(SoundSource source)
        {
            // Call your own OpenAL code here, for example:
            ALChecker.Check(() => AL.Source(source.Handle, ALSourcei.Buffer, 0));

            // Note that it call alSource() and being passed with lambda to ALChecker.Check()
            // ALChecker.Check will call the function that specified
            // After function terminate, it will check for OpenAL Error and printed to Cgen.Logger

            // Then proceed playing the sound
            base.Play(source);
        }
    }
```

In case you dislike lambda, perform error checking by calling `ALCheker.CheckError()` right after performing your OpenAL call, for example:

```
    AL.GetSource(source.Handle, ALSourcei.Buffer, 0);
    ALChecker.CheckError();
```

Any error will printed via `Cgen.Logger`. Check and explore `Cgen.Internal.OpenAL.ALChecker` and `Cgen.Logger` for more information.


## Dependencies ##

This library uses several dependencies to perform specific operations.
The dependencies are separated into 2 types: Internal and External:

- External dependencies are included under `Dependencies` folder and must be installed or shipped along with the application and located under same folder with the main of application. These dependencies may installed by default in certain Operating System. This folder also used to store Nuget packages (`Dependencies/Packages`).

- Internal dependencies are compiled along with this library during compilation, the source code is located under `Source\Cgen\Dependencies`

List of dependencies:
- [OpenTK](https://github.com/opentk/opentk)
- [NVorbis](https://github.com/SirusDoma/NVorbis)

## Version History ##

### v1.0
- Complete Rewrite of the API

### v0.8.5
- Added few Properties to `SoundBuffer`: `IsRelativeToListener`, `MinumumDistance`, `Attenuation`, `Position3D` and `Pan`
- Reworked `Volume` property: readjusted value, now the value range is between 0 (Mute) and 100 (Full)

### v0.8.3
- Added Supports for 32bit PCM, 32bit Float and 24bit PCM Wav Samples.

### v0.8.0
- Fixed various bugs when buffering the `SoundBuffer` object.
- Fixed instancing new `Sound` object bugs.
- `SoundSystem` now use streaming algorithm instead load all data into buffer to play the sounds.
- Added Automated Update Cycle of `SoundSystem`.
- Added <i>Deferred</i> audio streaming.
- Integrated `XRAM` and `EFX` Extension to the `SoundSystem`.

### v0.7.2
- Added `ISoundStreamReader` to implement custom audio decoder.
- Fixed minor bugs on `SoundSystem.Update(double)` cycle.

### v0.7.0
- Initial public release.

## License ##

This is an open-sourced library licensed under the [MIT License](http://github.com/SirusDoma/Cgen.Audio/blob/master/LICENSE).
