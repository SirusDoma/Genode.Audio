using System;
using System.Collections.Generic;
using System.Text;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents <see cref="SoundSource"/> states.
    /// </summary>
    public enum SoundStatus
    {
        /// <summary>
        /// Sound is just barely loaded.
        /// </summary>
        Initial,

        /// <summary>
        /// Sound is not playing.
        /// </summary>
        Stopped,
        
        /// <summary>
        /// Sound is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// Sound is playing.
        /// </summary>
        Playing
    }
}
