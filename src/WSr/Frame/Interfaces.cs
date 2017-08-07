using System;
using System.Collections.Generic;
using System.Linq;

using static WSr.ListConstruction;

namespace WSr.Frame
{
    /// <summary>
    /// A class that implements IFramereaderState<T> will take a byte and 
    /// produce a new state until it represent a completed T.
    /// </summary>
    public interface IFrameReaderState<T>
    {
        /// <summary>
        /// Indicates if the state represents a completed T or not. 
        /// </summary>
        /// <returns>True if the state is complete, false otherwise</returns>
        bool Complete { get; }
        /// <summary>
        /// Returns what the state has built up so far
        /// </summary>
        /// <returns></returns>
        T Reading { get; }
        /// <summary>
        /// Take a byte and return a state that represents that bytes contribution towards a completed state.
        /// </summary>
        /// <returns>The new state</returns>
        Func<byte, IFrameReaderState<T>> Next { get; }
    }
}