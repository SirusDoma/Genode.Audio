# Cgen.Audio #
Audio Submodule of CygnusJam Game Engine.

- **Author**: SirusDoma
- **Email**: com@cxo2.me
- **Latest Version**: 2.0

## Summary ##

Cgen.Audio is a simple yet powerful Cross-platform Audio Engine which is provides audio playbacks.
Written under C# language based on OpenAL (with [OpenTK](https://github.com/opentk/opentk) binding).  

The main idea is to provide an audio playback that simple and fast. If you prefer simplicity over fancy features, then this audio engine is for you.  
This project is rewritten .NET Standard and will no longer supporting initial framework target, which is .NET Framework 2.0  

## Compiling Project ##

The project framework is targeted to .NET Standard to ensure maximum compatibility between .NET 4.7 and .NET Core projects. This framework also make a use of OpenTK.NETCore, which is the official OpenTK package for .NET Core. Moreover, this library itself written purely in managed C#, including encoders and decoders. But still, it may use native dependencies, such as OpenAL. See [Dependencies](#dependencies) for more details.

It is also required to configure the Build Configuration Platform (`x86`/`x64`) of target application to match the library configuration. Avoid using `Any CPU` platform, because this framework uses native external dependencies (e.g: the engine may fail when deciding which version of `openal32.dll` to use).  

## Documentation ##

Currently, the engine doesn't have detailed API reference and therefore, the basic usages is covered along with this document.  
You're welcomed to contribute the API Documentation, submit pull request and I'll be happy to take care the rest!  

### 1. Initialization ###

The `SoundSystem` engine is actually initialized on the fly, you don't even have to call any method to initialize or choosing which audio device that you should pick or configuring specific hardware dependent configuration. The engine will setup everything automagically.  

In case you want to initialize the `SoundSystem` explicitly, call `SoundSystem.Instance.Initialize()`

### 2. Handling Update Cycle ###

As described in Usages Notes, the `SoundSystem` need to updated frequently in order to stream the sound buffer.  

Technically, `SoundSystem` handle `SoundSource` lifecycle by poolling of stopped and un-used instances to overcome instancing limitation, this allow you to have `Sound` and `Music` instances as many as you wish.  

However, maximum `SoundSource`'s that can be played at the same time still exists, the hard limit of `SoundSource`'s may fairly different across multiple platforms, however the engine put constant hard limit up to **256 sources**. This number should be more than enough for all cases, even if you're trying to simulate an ochestra.  

Unlike the previous version of API, you will need call `SoundSystem.Instance.Update()` manually and regularly. Also, **You have to call the `SoundSystem.Instance.Update()` at the same thread as you play the `Sound` or `Music`**. In other words, you should **NOT** perform `SoundSystem.Instance.Play(sound)` or `SoundSystem.Instance.Play(music)` at different thread than update cycle!  

This due to the engine limitation that cannot protect the `SoundSource` from thread race with mutex without suffering a stutter. However, you're still allowed to play the `Sound` and `Music` and call `SoundSystem.Instance.Update()` in a separate thread, as long they share same thread, you also may want to call `AudioDevice.MakeCurrent()` to make OpenAL running on your thread. 

Following codes is a good example if you're using separate thread to handle `SoundSystem` update cycle:

```
    // Initialize music instance
    var music = new Music('Path/To/Music.ogg');

    // Launch new thread that play and update the system
    var thread = new Thread(() => UpdateSystem(music));
    thread.Start();
    
    // And the thread function
    private void UpdateSystem(Music music)
    {
        // Make OpenAL to current thread
        AudioDevice.MakeCurrent();

        // Play the music
        SoundSystem.Instance.Play(music);

        // Update SoundSystem while music is playing
        while (music.Status != SoundStatus.Stopped)
        {
            SoundSystem.Instance.Update();
        }
    }

```

Ignoring update cycle of `SoundSystem` may cause undefined behavior, especially when playing `Music` or playing multiple instance of `Sound`.

### 3. Creating a Sound / Music object ###

Either `Sound` or `Music`, both inherit `SoundSource` elements, which provides basic audio properties and behaviors across audio implementation. In other words, you can implement your own Audio implementation by inherit `SoundSource` abstract class.

`Sound` is a lightweight object that plays loaded audio data from a `SoundBuffer`. The audio data (or buffer) is loaded directly to memory as whole. Thus, should be used for small sounds that can fit in memory and should suffer no lag when they are played.  

For example `Sound` is great for effect like door bells, or game effect such as gun shot, footstep, etc.
In order to create `Sound`, `SoundBuffer` is required, you can construct `SoundBuffer` from file, `Stream` or even an array of bytes. Note that `SoundBuffer` can be used by multiple instance of `Sound`. Do **NOT** create multiple `SoundBuffer` with same audio data, reuse it instead to save memory.

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

`Music` doesn't load all the audio data into memory, instead it streams it on the fly from the source file / data. It is typically used to play compressed music that lasts several minutes, and would otherwise take many seconds to load and take hundreds of MB in memory due large amount of decoded samples.

Constructing `Music` object straightforward rather than `Sound` object, you don't have to create `SoundBuffer` because `Music` will stream instead load whole samples directly into memory.

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
```

Note that it is recommended that you leave the `Sound` and `Music` without disposing them. See [Cleanup](#cleanup) section for more information

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

### 7. Extending Sound ###

`SoundStream` can be extended to suit your needs, this allow you to stream data from various source that `Music` cannot comprehend, such as network stream or audio recorder.

In such cases, you should derive `SoundStream`. Following abstract methods need to be implemented:

- `void Initialize(int channelCount, int sampleRate)`
  You have to call this method. Depending at your implementation, this can be called anywhere (of course before the stream begin), but most of time, it is called right after your source stream / decoder is open and / or read partial information of the source audio data to gather `channelCount` and `sampleRate`.

- `bool OnGetData(out short[] samples)`  
  You have to override this method. It is responsible to read the samples from the source and will be called when the `SoundStream` is out of buffer to process. You will have to return boolean to indicate whether there is remaining sample to be streamed.

- `void OnSeek(TimeSpan time)`
  You have to override this method. This method will be called when `PlayingOffset` is changed by user, you will have to seek the position of source stream / decoder to the equally same as specified time. Sometimes it is impossible to seek the stream in certain cases, such as voice call. At that case, you can simply return the method and inform the user that seek is not available at current implementation.

In addition to `SoundStream`, you cannot extend `Sound` class because it is fairly simple object that load `SoundBuffer` into memory. You can inherit `SoundSource` instead make your own Sound class, however you may need provide low level function during the implementation, such as providing buffer data to OpenAL handle manually.  

In case you want to cover audio data that not supported or defined within library, then make your own `SoundDecoder` instead by inherit it and register it to `Decoders` class.

### 8. Custom Decoder / Encoder ###

In case the library doesn't provide audio processor that you need, you can always make your own Decoder and Encoder. Encoder and Decoder share same structure unless Decoder `Read()` the samples and Encoder `Write()` the samples instead.  

You'll have to provide following implementations:

- `bool Check(Stream stream)`  
  You have to override this method and provide implementation to check whether the given `Stream` is accepted by `SoundDecoder` / `SoundEncoder` instance, return `true` if the encoder / decoder able to comprehend the format, otherwise `false`.

- `void Initialize(int channelCount, int sampleRate)`
  This method is responsible to `Initialize` your decoder / encoder from given `Stream` in the constructor, You don't need to check the `BaseStream` as it was previously checked.

### 9. Extending SoundSystem ###

In addition to `SoundStream`, it also possible to extend the `SoundSystem` to perform customized code. In case you want to perform OpenAL specific codes, you need to provide your own or use existing OpenAL library. This audio engine uses [OpenTK](https://github.com/opentk/opentk) to perform OpenAL operations, however it is completely up to you to choose OpenAL implementation.  

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

Cgen.Audio provides OpenAL Error Checker. It is part of low level module, but it exposed to public and you can use it.

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

Any error will printed via `Cgen.Logger`. 
Check the sources in case you're curious about how it works.

### 10. Audio Capture ###

You can also record audio from your audio input device such as Microphone. use `SoundBufferRecorder` to record audio to `SoundBuffer`, you can also make your own implementation by implement `SoundRecorder` class. Basically, you only want do this when you don't want to save the recorded audio on `SoundBuffer` and / or prefer to stream it instead, such as VoIP.

The API very similar to playback API, but you will need to use `Capture()` instead of `Play()`.

## Cleanup ##

It is recommended that you do not disposing `SoundSource` (`Sound` or `Music`) right after it is not used, Disposing `SoundSource` will releasing OpenAL handle, which mean it cannot be reused and recycled by pool system of `SoundSystem`.    

Disposing of `SoundSource` will not give you significant benefit both in term of memory usage or cpu utilization. Because the buffer will be enqueued anyway to save memory and keep `SoundSource` state reusable while the OpenAL handle itself only take small amount of bytes.  

In addition to disposing OpenAL Handle, the OpenAL sound buffer data will be disposed as well. OpenAL buffers often referenced by multiple amount of `SoundSource` instances, especially `Sound` class. The library itself automatically re-generate buffers from decoded audio data when needed, but at very rare-cases, such as threading race, this may lead to an error when multple sources share same `SoundBuffer` (or other OpenAL buffers), most likely they will fail to load and / or play.  

Still, you can dispose it anyway, the disposed handle won't be preserved in the pool and will be enqueued from playing pool and unused pool to prevent reused again by another source. Remember, this may impact the performance of the application, as deleting and recreating handle is more expensive than reusing unused source.  

Only dispose when you're sure that the `SoundSource` instances are no longer needed and the buffers must be released.  
Also, in case you're about to exit the program or library is no longer needed, it is recommended to perform the cleanup.  

```
    // Dispose SoundSystem
    SoundSystem.Instance.Dispose();
```

Note this will stop all playing sounds and music instances and dispose them, including the OpenAL handle that mentioned earlier.  

Performing any playback operation or modifying audio properties is no longer safe after this point.

## Dependencies ##

This library uses several dependencies to perform specific operations.
The dependencies are separated into 2 types: Internal and External:

- External dependencies are included under `Dependencies` folder and must be installed or shipped along with the application and located under same folder with the main of application. These dependencies may installed by default in certain Operating System. This folder also used to store Nuget packages (`Dependencies/Packages`).

- Internal dependencies are compiled along with this library during compilation, the source code is located under `Source\Cgen\Dependencies`

List of dependencies:
- [OpenTK](https://github.com/opentk/opentk)
- [NVorbis](https://github.com/SirusDoma/NVorbis)
- [Ogg Vorbis Encoder](https://github.com/SteveLillis/.NET-Ogg-Vorbis-Encoder)

## Version History ##

### v2.0
This release contains minor breaking changes  
- Reworked Sound Decoder
- Added Sound Encoder
- Added Sound Recorder
- Minor bugfix
- Added some options and features to AudioDevice

### v1.5
This release contains minor breaking changes  
- Reworked `SoundSystem` `SoundSource` pooling
- Reworked `SoundSource` audio states
- Removed threading mechanic from `Music` and `SoundSystem`
- Renamed `SoundReader` to `SoundDecoder`
- Updated small amounts of OpenAL functions and states

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
