using AprCSTyrian.Core.Ports;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary><see cref="IClock"/> 的 SDL2 實作，直接對應 SDL_GetTicks / SDL_Delay。</summary>
internal sealed class SdlClock : IClock
{
    public uint Ticks => SDL.SDL_GetTicks();

    public void Delay(uint milliseconds) => SDL.SDL_Delay(milliseconds);
}
