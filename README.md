# Genode.Audio #
Audio Submodule of CygnusJam Game Engine.

- **Author**: SirusDoma
- **Email**: com@cxo2.me
- **Latest Version**: 3.0.5

## Summary ##

Genode.Audio is a simple yet powerful Cross-platform Audio Engine which is provides audio playbacks.
Written under C# language based on OpenAL (with [OpenTK](https://github.com/opentk/opentk) binding). The main idea is to provide a low-latency audio playback that simple and fast. If you prefer simplicity over fancy features, then this audio engine is for you.  

This project has been rewritten to be compatibile with .NET Standard and will no longer supporting initial framework target, which is .NET Framework 2.0  

## Compiling Project ##

The project framework is targeted to .NET Standard to ensure maximum compatibility between .NET 4.7 and .NET Core projects. Moreover, this library itself written purely in managed C#, including encoders and decoders but still depend on native dependencies, such as OpenAL. See [Dependencies](#dependencies) for more details.

It is also required to configure the Build Configuration Platform (`x86`/`x64`) of target application to match the library configuration. Avoid using `Any CPU` platform, because this framework uses native external dependencies (e.g: the engine may fail when deciding which version of `openal32.dll` to use).  

## Documentation ##

For more information about the audio API, click [here](https://github.com/SirusDoma/Genode.Audio/wiki).  
You can check the sample program [here](https://github.com/SirusDoma/Genode.Audio/tree/master/Source/Example).

## Dependencies ##

This library uses several dependencies to perform specific operations.
The dependencies are separated into 2 types: Internal and External:

- External dependencies are included under `Dependencies` folder and must be installed or shipped along with the application and located under same folder with the main of application. These dependencies may installed by default in certain Operating System. This folder also used to store Nuget packages (`Dependencies/Packages`).

- Internal dependencies are compiled along with this library during compilation, the source code is located under `Source\Genode.Audio\Dependencies`

List of dependencies:
- [OpenTK](https://github.com/opentk/opentk)
- [NVorbis](https://github.com/SirusDoma/NVorbis)
- [Ogg Vorbis Encoder](https://github.com/SteveLillis/.NET-Ogg-Vorbis-Encoder)

## Version History ##

### v3.0.7
- Upgrade NVorbis dependency, now support .NET Standard natively
- CoreRT build is now supported

### v3.0.5
- Add support to play custom `SoundStream` implementation
- Fix source pooling system not generating native audio handle when playing / replaying `SoundChannel`
- Fix internal error when disposing invalid native audio handle
- Fix misconfigured access modifier of `SoundRecorder` and `SoundSystem` API
- `SoundRecorder` now run on it's own thread

### v3.0
This release contains major breaking changes  
- Complete Rewrite API

### v2.0.5
- Improved `SoundSystem` pool handling
- Improved `SoundStream` buffers handling and processing
- Encoder now expose `SampleInfo` to determine sample meta data
- Added `Pause()`, `Resume()` and `Stop()` to manipulate all playing `SoundSource` instances
- Minor Bugfixes

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
- Added Supports for 32bit PCM, 32bit Float and 24bit PCM Wav Samples

### v0.8.0
- Fixed various bugs when buffering the `SoundBuffer` object
- Fixed instancing new `Sound` object bugs
- `SoundSystem` now use streaming algorithm instead load all data into buffer to play the sounds
- Added Automated Update Cycle of `SoundSystem`
- Added <i>Deferred</i> audio streaming
- Integrated `XRAM` and `EFX` Extension to the `SoundSystem`

### v0.7.2
- Added `ISoundStreamReader` to implement custom audio decoder
- Fixed minor bugs on `SoundSystem.Update(double)` cycle

### v0.7.0
- Initial public release

## License ##

This is an open-sourced library licensed under the [MIT License](http://github.com/SirusDoma/Cgen.Audio/blob/master/LICENSE)
