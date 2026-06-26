namespace AprCSTyrian.Core;

/// <summary>對應 lds_play.h:SoundBank（樂器定義）。</summary>
internal sealed class SoundBank
{
    public byte mod_misc, mod_vol, mod_ad, mod_sr, mod_wave,
        car_misc, car_vol, car_ad, car_sr, car_wave, feedback, keyoff,
        portamento, glide, finetune, vibrato, vibdelay, mod_trem, car_trem,
        tremwait, arpeggio;
    public readonly byte[] arp_tab = new byte[12];
    public ushort start, size;
    public byte fms;
    public ushort transp;
    public byte midinst, midvelo, midkey, midtrans, middum1, middum2;
}

/// <summary>對應 lds_play.h:Channel（播放通道狀態）。</summary>
internal sealed class Channel
{
    public ushort gototune, lasttune, packpos;
    public byte finetune, glideto, portspeed, nextvol, volmod, volcar,
        vibwait, vibspeed, vibrate, trmstay, trmwait, trmspeed, trmrate, trmcount,
        trcwait, trcspeed, trcrate, trccount, arp_size, arp_speed, keycount,
        vibcount, arp_pos, arp_count, packwait;
    public readonly byte[] arp_tab = new byte[12];
    // chancheat
    public byte cc_chandelay, cc_sound;
    public ushort cc_high;
}

/// <summary>對應 lds_play.h:Position。</summary>
internal struct Position
{
    public ushort patnum;
    public byte transpose;
}

/// <summary>
/// 移植 sources/src/lds_play.c —— Loudness .lds 音樂格式播放器（改編自 adplug）。
/// 驅動 <see cref="Opl"/> 的 OPL2 暫存器。
/// </summary>
internal static class Lds
{
    private static readonly byte[] op_table = { 0x00, 0x01, 0x02, 0x08, 0x09, 0x0a, 0x10, 0x11, 0x12 };

    private static readonly ushort[] frequency = {
        343,344,345,347,348,349,350,352,353,354,356,357,358,
        359,361,362,363,365,366,367,369,370,371,373,374,375,
        377,378,379,381,382,384,385,386,388,389,391,392,393,
        395,396,398,399,401,402,403,405,406,408,409,411,412,
        414,415,417,418,420,421,423,424,426,427,429,430,432,
        434,435,437,438,440,442,443,445,446,448,450,451,453,
        454,456,458,459,461,463,464,466,468,469,471,473,475,
        476,478,480,481,483,485,487,488,490,492,494,496,497,
        499,501,503,505,506,508,510,512,514,516,518,519,521,
        523,525,527,529,531,533,535,537,538,540,542,544,546,
        548,550,552,554,556,558,560,562,564,566,568,571,573,
        575,577,579,581,583,585,587,589,591,594,596,598,600,
        602,604,607,609,611,613,615,618,620,622,624,627,629,
        631,633,636,638,640,643,645,647,650,652,654,657,659,
        662,664,666,669,671,674,676,678,681,683
    };

    private static readonly byte[] vibtab = {
        0,13,25,37,50,62,74,86,98,109,120,131,142,152,162,
        171,180,189,197,205,212,219,225,231,236,240,244,247,
        250,252,254,255,255,255,254,252,250,247,244,240,236,
        231,225,219,212,205,197,189,180,171,162,152,142,131,
        120,109,98,86,74,62,50,37,25,13
    };

    private static readonly byte[] tremtab = {
        0,0,1,1,2,4,5,7,10,12,15,18,21,25,29,33,37,42,47,
        52,57,62,67,73,79,85,90,97,103,109,115,121,128,134,
        140,146,152,158,165,170,176,182,188,193,198,203,208,
        213,218,222,226,230,234,237,240,243,245,248,250,251,
        253,254,254,255,255,255,254,254,253,251,250,248,245,
        243,240,237,234,230,226,222,218,213,208,203,198,193,
        188,182,176,170,165,158,152,146,140,134,127,121,115,
        109,103,97,90,85,79,73,67,62,57,52,47,42,37,33,29,
        25,21,18,15,12,10,7,5,4,2,1,1,0
    };

    private const ushort maxsound = 0x3f, maxpos = 0xff;

    private static SoundBank[] soundbank = Array.Empty<SoundBank>();
    private static readonly Channel[] channel = NewChannels();
    private static Position[] positions = Array.Empty<Position>();

    private static readonly byte[] fmchip = new byte[0xff];
    private static byte jumping, fadeonoff, allvolume, hardfade, tempo_now, pattplay, tempo, regbd, mode, pattlen;
    private static readonly byte[] chandelay = new byte[9];
    private static ushort posplay, jumppos, speed;
    private static ushort[] patterns = Array.Empty<ushort>();
    private static ushort numpatch, numposi, mainvolume;

    public static bool playing, songlooped;

    private static Channel[] NewChannels()
    {
        var a = new Channel[9];
        for (int i = 0; i < 9; i++) a[i] = new Channel();
        return a;
    }

    private static ushort FREQ(int tune)
    {
        int m = tune % (12 * 16);
        if (m < 0) m += 12 * 16;
        return frequency[m];
    }

    public static bool lds_load(Stream f, uint music_offset, uint music_size)
    {
        f.Seek(music_offset, SeekOrigin.Begin);

        mode = CFile.read_u8(f);
        if (mode > 2)
        {
            Console.Error.WriteLine("error: failed to load music");
            return false;
        }
        speed = CFile.read_u16(f);
        tempo = CFile.read_u8(f);
        pattlen = CFile.read_u8(f);
        for (int i = 0; i < 9; i++) chandelay[i] = CFile.read_u8(f);
        regbd = CFile.read_u8(f);

        numpatch = CFile.read_u16(f);
        soundbank = new SoundBank[numpatch];
        for (int i = 0; i < numpatch; i++)
        {
            var sb = new SoundBank();
            sb.mod_misc = CFile.read_u8(f); sb.mod_vol = CFile.read_u8(f);
            sb.mod_ad = CFile.read_u8(f); sb.mod_sr = CFile.read_u8(f); sb.mod_wave = CFile.read_u8(f);
            sb.car_misc = CFile.read_u8(f); sb.car_vol = CFile.read_u8(f);
            sb.car_ad = CFile.read_u8(f); sb.car_sr = CFile.read_u8(f); sb.car_wave = CFile.read_u8(f);
            sb.feedback = CFile.read_u8(f); sb.keyoff = CFile.read_u8(f);
            sb.portamento = CFile.read_u8(f); sb.glide = CFile.read_u8(f); sb.finetune = CFile.read_u8(f);
            sb.vibrato = CFile.read_u8(f); sb.vibdelay = CFile.read_u8(f);
            sb.mod_trem = CFile.read_u8(f); sb.car_trem = CFile.read_u8(f); sb.tremwait = CFile.read_u8(f);
            sb.arpeggio = CFile.read_u8(f);
            for (int j = 0; j < 12; j++) sb.arp_tab[j] = CFile.read_u8(f);
            sb.start = CFile.read_u16(f); sb.size = CFile.read_u16(f);
            sb.fms = CFile.read_u8(f); sb.transp = CFile.read_u16(f);
            sb.midinst = CFile.read_u8(f); sb.midvelo = CFile.read_u8(f); sb.midkey = CFile.read_u8(f);
            sb.midtrans = CFile.read_u8(f); sb.middum1 = CFile.read_u8(f); sb.middum2 = CFile.read_u8(f);
            soundbank[i] = sb;
        }

        numposi = CFile.read_u16(f);
        positions = new Position[9 * numposi];
        for (int i = 0; i < numposi; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                ushort patnum = CFile.read_u16(f);
                byte transpose = CFile.read_u8(f);
                positions[i * 9 + j].patnum = (ushort)(patnum / 2);
                positions[i * 9 + j].transpose = transpose;
            }
        }

        f.Seek(2, SeekOrigin.Current); // ignore # of digital sounds

        uint remaining = music_size - (uint)(f.Position - music_offset);
        int numpatterns = (int)(remaining / 2);
        patterns = new ushort[numpatterns];
        for (int i = 0; i < numpatterns; i++) patterns[i] = CFile.read_u16(f);

        lds_rewind();
        return true;
    }

    public static void lds_free()
    {
        soundbank = Array.Empty<SoundBank>();
        positions = Array.Empty<Position>();
        patterns = Array.Empty<ushort>();
    }

    public static void lds_rewind()
    {
        tempo_now = 3;
        playing = true; songlooped = false;
        jumping = fadeonoff = allvolume = hardfade = pattplay = 0;
        posplay = jumppos = mainvolume = 0;
        for (int i = 0; i < 9; i++) channel[i] = new Channel();
        Array.Clear(fmchip, 0, fmchip.Length);

        Opl.opl_init();
        Opl.opl_write(1, 0x20);
        Opl.opl_write(8, 0);
        Opl.opl_write(0xbd, regbd);

        for (int i = 0; i < 9; i++)
        {
            Opl.opl_write(0x20 + op_table[i], 0);
            Opl.opl_write(0x23 + op_table[i], 0);
            Opl.opl_write(0x40 + op_table[i], 0x3f);
            Opl.opl_write(0x43 + op_table[i], 0x3f);
            Opl.opl_write(0x60 + op_table[i], 0xff);
            Opl.opl_write(0x63 + op_table[i], 0xff);
            Opl.opl_write(0x80 + op_table[i], 0xff);
            Opl.opl_write(0x83 + op_table[i], 0xff);
            Opl.opl_write(0xe0 + op_table[i], 0);
            Opl.opl_write(0xe3 + op_table[i], 0);
            Opl.opl_write(0xa0 + i, 0);
            Opl.opl_write(0xb0 + i, 0);
            Opl.opl_write(0xc0 + i, 0);
        }
    }

    public static void lds_fade(byte spd) => fadeonoff = spd;

    public static void lds_setregs(byte reg, byte val)
    {
        if (fmchip[reg] == val) return;
        fmchip[reg] = val;
        Opl.opl_write(reg, val);
    }

    public static void lds_setregs_adv(byte reg, byte mask, byte val)
    {
        lds_setregs(reg, (byte)((fmchip[reg] & mask) | val));
    }

    public static int lds_update()
    {
        if (!playing) return 0;

        // handle fading
        if (fadeonoff != 0)
        {
            if (fadeonoff <= 128)
            {
                if (allvolume > fadeonoff || allvolume == 0)
                {
                    allvolume -= fadeonoff;
                }
                else
                {
                    allvolume = 1;
                    fadeonoff = 0;
                    if (hardfade != 0)
                    {
                        playing = false;
                        hardfade = 0;
                        for (int i = 0; i < 9; i++) channel[i].keycount = 1;
                    }
                }
            }
            else
            {
                if ((byte)((allvolume + (0x100 - fadeonoff)) & 0xff) <= mainvolume)
                    allvolume = (byte)(allvolume + (0x100 - fadeonoff));
                else
                {
                    allvolume = (byte)mainvolume;
                    fadeonoff = 0;
                }
            }
        }

        // handle channel delay
        for (int chan = 0; chan < 9; chan++)
        {
            Channel c = channel[chan];
            if (c.cc_chandelay != 0)
            {
                if (--c.cc_chandelay == 0)
                    lds_playsound(c.cc_sound, chan, c.cc_high);
            }
        }

        // handle notes
        if (tempo_now == 0 && positions.Length != 0)
        {
            bool vbreak = false;
            for (int chan = 0; chan < 9; chan++)
            {
                Channel c = channel[chan];
                if (c.packwait == 0)
                {
                    ushort patnum = positions[posplay * 9 + chan].patnum;
                    byte transpose = positions[posplay * 9 + chan].transpose;

                    ushort comword = patterns[patnum + c.packpos];
                    byte comhi = (byte)(comword >> 8);
                    byte comlo = (byte)(comword & 0xff);
                    if (comword != 0)
                    {
                        if (comhi == 0x80)
                        {
                            c.packwait = comlo;
                        }
                        else if (comhi >= 0x80)
                        {
                            switch (comhi)
                            {
                            case 0xff:
                                c.volcar = (byte)((((c.volcar & 0x3f) * comlo) >> 6) & 0x3f);
                                if ((fmchip[0xc0 + chan] & 1) != 0)
                                    c.volmod = (byte)((((c.volmod & 0x3f) * comlo) >> 6) & 0x3f);
                                break;
                            case 0xfe: tempo = (byte)(comword & 0x3f); break;
                            case 0xfd: c.nextvol = comlo; break;
                            case 0xfc: playing = false; break;
                            case 0xfb: c.keycount = 1; break;
                            case 0xfa: vbreak = true; jumppos = (ushort)((posplay + 1) & maxpos); break;
                            case 0xf9:
                                vbreak = true;
                                jumppos = (ushort)(comlo & maxpos);
                                jumping = 1;
                                if (jumppos < posplay) songlooped = true;
                                break;
                            case 0xf8: c.lasttune = 0; break;
                            case 0xf7:
                                c.vibwait = 0;
                                c.vibspeed = (byte)((comlo >> 4) + 2);
                                c.vibrate = (byte)((comlo & 15) + 1);
                                break;
                            case 0xf6: c.glideto = comlo; break;
                            case 0xf5: c.finetune = comlo; break;
                            case 0xf4:
                                if (hardfade == 0) { allvolume = comlo; mainvolume = comlo; fadeonoff = 0; }
                                break;
                            case 0xf3:
                                if (hardfade == 0) fadeonoff = comlo;
                                break;
                            case 0xf2: c.trmstay = comlo; break;
                            case 0xf1: // panorama
                            case 0xf0: // progch (MIDI, unhandled)
                                break;
                            default:
                                if (comhi < 0xa0) c.glideto = (byte)(comhi & 0x1f);
                                break;
                            }
                        }
                        else
                        {
                            byte sound;
                            ushort high;
                            sbyte transp = (sbyte)(transpose & 127);
                            if ((transpose & 64) != 0) transp |= unchecked((sbyte)128);

                            if ((transpose & 128) != 0)
                            {
                                sound = (byte)((comlo + transp) & maxsound);
                                high = (ushort)(comhi << 4);
                            }
                            else
                            {
                                sound = (byte)(comlo & maxsound);
                                high = (ushort)((comhi + transp) << 4);
                            }

                            if (chandelay[chan] == 0)
                            {
                                lds_playsound(sound, chan, high);
                            }
                            else
                            {
                                c.cc_chandelay = chandelay[chan];
                                c.cc_sound = sound;
                                c.cc_high = high;
                            }
                        }
                    }

                    c.packpos++;
                }
                else
                {
                    c.packwait--;
                }
            }

            tempo_now = tempo;
            pattplay++;
            if (vbreak)
            {
                pattplay = 0;
                for (int i = 0; i < 9; i++) { channel[i].packpos = channel[i].packwait = 0; }
                posplay = jumppos;
            }
            else
            {
                if (pattplay >= pattlen)
                {
                    pattplay = 0;
                    for (int i = 0; i < 9; i++) { channel[i].packpos = channel[i].packwait = 0; }
                    posplay = (ushort)((posplay + 1) & maxpos);
                }
            }
        }
        else
        {
            tempo_now--;
        }

        // make effects
        for (int chan = 0; chan < 9; chan++)
        {
            Channel c = channel[chan];
            byte regnum = op_table[chan];
            if (c.keycount > 0)
            {
                if (c.keycount == 1) lds_setregs_adv((byte)(0xb0 + chan), 0xdf, 0);
                c.keycount--;
            }

            // arpeggio
            ushort arpreg;
            if (c.arp_size == 0)
                arpreg = 0;
            else
            {
                arpreg = (ushort)(c.arp_tab[c.arp_pos] << 4);
                if (arpreg == 0x800)
                {
                    if (c.arp_pos > 0) c.arp_tab[0] = c.arp_tab[c.arp_pos - 1];
                    c.arp_size = 1; c.arp_pos = 0;
                    arpreg = (ushort)(c.arp_tab[0] << 4);
                }

                if (c.arp_count == c.arp_speed)
                {
                    c.arp_pos++;
                    if (c.arp_pos >= c.arp_size) c.arp_pos = 0;
                    c.arp_count = 0;
                }
                else
                    c.arp_count++;
            }

            ushort freq, octave, tune;
            // glide & portamento
            if (c.lasttune != 0 && (c.lasttune != c.gototune))
            {
                if (c.lasttune > c.gototune)
                {
                    if (c.lasttune - c.gototune < c.portspeed) c.lasttune = c.gototune;
                    else c.lasttune -= c.portspeed;
                }
                else
                {
                    if (c.gototune - c.lasttune < c.portspeed) c.lasttune = c.gototune;
                    else c.lasttune += c.portspeed;
                }

                int t;
                if (arpreg >= 0x800) t = c.lasttune - (arpreg ^ 0xff0) - 16;
                else t = c.lasttune + arpreg;

                freq = FREQ(t);
                octave = (ushort)(t / (12 * 16) - 1);
                lds_setregs((byte)(0xa0 + chan), (byte)(freq & 0xff));
                lds_setregs_adv((byte)(0xb0 + chan), 0x20, (byte)(((octave << 2) + (freq >> 8)) & 0xdf));
            }
            else
            {
                // vibrato
                if (c.vibwait == 0)
                {
                    if (c.vibrate != 0)
                    {
                        ushort wibc = (ushort)(vibtab[c.vibcount & 0x3f] * c.vibrate);

                        if ((c.vibcount & 0x40) == 0) tune = (ushort)(c.lasttune + (wibc >> 8));
                        else tune = (ushort)(c.lasttune - (wibc >> 8));

                        int t;
                        if (arpreg >= 0x800) t = tune - (arpreg ^ 0xff0) - 16;
                        else t = tune + arpreg;

                        freq = FREQ(t);
                        octave = (ushort)(t / (12 * 16) - 1);
                        lds_setregs((byte)(0xa0 + chan), (byte)(freq & 0xff));
                        lds_setregs_adv((byte)(0xb0 + chan), 0x20, (byte)(((octave << 2) + (freq >> 8)) & 0xdf));
                        c.vibcount += c.vibspeed;
                    }
                    else if (c.arp_size != 0)
                    {
                        int t;
                        if (arpreg >= 0x800) t = c.lasttune - (arpreg ^ 0xff0) - 16;
                        else t = c.lasttune + arpreg;

                        freq = FREQ(t);
                        octave = (ushort)(t / (12 * 16) - 1);
                        lds_setregs((byte)(0xa0 + chan), (byte)(freq & 0xff));
                        lds_setregs_adv((byte)(0xb0 + chan), 0x20, (byte)(((octave << 2) + (freq >> 8)) & 0xdf));
                    }
                }
                else
                {
                    c.vibwait--;

                    if (c.arp_size != 0)
                    {
                        int t;
                        if (arpreg >= 0x800) t = c.lasttune - (arpreg ^ 0xff0) - 16;
                        else t = c.lasttune + arpreg;

                        freq = FREQ(t);
                        octave = (ushort)(t / (12 * 16) - 1);
                        lds_setregs((byte)(0xa0 + chan), (byte)(freq & 0xff));
                        lds_setregs_adv((byte)(0xb0 + chan), 0x20, (byte)(((octave << 2) + (freq >> 8)) & 0xdf));
                    }
                }
            }

            byte level;
            ushort tremc;
            // tremolo (modulator)
            if (c.trmwait == 0)
            {
                if (c.trmrate != 0)
                {
                    tremc = (ushort)(tremtab[c.trmcount & 0x7f] * c.trmrate);
                    if ((tremc >> 8) <= (c.volmod & 0x3f)) level = (byte)((c.volmod & 0x3f) - (tremc >> 8));
                    else level = 0;

                    if (allvolume != 0 && (fmchip[0xc0 + chan] & 1) != 0)
                        lds_setregs_adv((byte)(0x40 + regnum), 0xc0, (byte)(((level * allvolume) >> 8) ^ 0x3f));
                    else
                        lds_setregs_adv((byte)(0x40 + regnum), 0xc0, (byte)(level ^ 0x3f));
                    c.trmcount += c.trmspeed;
                }
                else if (allvolume != 0 && (fmchip[0xc0 + chan] & 1) != 0)
                    lds_setregs_adv((byte)(0x40 + regnum), 0xc0, (byte)((((((c.volmod & 0x3f) * allvolume) >> 8) ^ 0x3f)) & 0x3f));
                else
                    lds_setregs_adv((byte)(0x40 + regnum), 0xc0, (byte)((c.volmod ^ 0x3f) & 0x3f));
            }
            else
            {
                c.trmwait--;
                if (allvolume != 0 && (fmchip[0xc0 + chan] & 1) != 0)
                    lds_setregs_adv((byte)(0x40 + regnum), 0xc0, (byte)((((((c.volmod & 0x3f) * allvolume) >> 8) ^ 0x3f)) & 0x3f));
            }

            // tremolo (carrier)
            if (c.trcwait == 0)
            {
                if (c.trcrate != 0)
                {
                    tremc = (ushort)(tremtab[c.trccount & 0x7f] * c.trcrate);
                    if ((tremc >> 8) <= (c.volcar & 0x3f)) level = (byte)((c.volcar & 0x3f) - (tremc >> 8));
                    else level = 0;

                    if (allvolume != 0)
                        lds_setregs_adv((byte)(0x43 + regnum), 0xc0, (byte)(((level * allvolume) >> 8) ^ 0x3f));
                    else
                        lds_setregs_adv((byte)(0x43 + regnum), 0xc0, (byte)(level ^ 0x3f));
                    c.trccount += c.trcspeed;
                }
                else if (allvolume != 0)
                    lds_setregs_adv((byte)(0x43 + regnum), 0xc0, (byte)((((((c.volcar & 0x3f) * allvolume) >> 8) ^ 0x3f)) & 0x3f));
                else
                    lds_setregs_adv((byte)(0x43 + regnum), 0xc0, (byte)((c.volcar ^ 0x3f) & 0x3f));
            }
            else
            {
                c.trcwait--;
                if (allvolume != 0)
                    lds_setregs_adv((byte)(0x43 + regnum), 0xc0, (byte)((((((c.volcar & 0x3f) * allvolume) >> 8) ^ 0x3f)) & 0x3f));
            }
        }

        return (!playing || songlooped) ? 0 : 1;
    }

    public static void lds_playsound(int inst_number, int channel_number, int tunehigh)
    {
        Channel c = channel[channel_number];
        SoundBank i = soundbank[inst_number];
        byte regnum = op_table[channel_number];
        byte volcalc, octave;
        ushort freq;

        // set fine tune
        tunehigh += ((i.finetune + c.finetune + 0x80) & 0xff) - 0x80;

        // arpeggio handling
        if (i.arpeggio == 0)
        {
            ushort arpcalc = (ushort)(i.arp_tab[0] << 4);
            if (arpcalc > 0x800) tunehigh = tunehigh - (arpcalc ^ 0xff0) - 16;
            else tunehigh += arpcalc;
        }

        // glide handling
        if (c.glideto != 0)
        {
            c.gototune = (ushort)tunehigh;
            c.portspeed = c.glideto;
            c.glideto = c.finetune = 0;
            return;
        }

        // set modulator registers
        lds_setregs((byte)(0x20 + regnum), i.mod_misc);
        volcalc = i.mod_vol;
        if (c.nextvol == 0 || (i.feedback & 1) == 0)
            c.volmod = volcalc;
        else
            c.volmod = (byte)((volcalc & 0xc0) | (((volcalc & 0x3f) * c.nextvol) >> 6));

        if ((i.feedback & 1) == 1 && allvolume != 0)
            lds_setregs((byte)(0x40 + regnum), (byte)(((c.volmod & 0xc0) | (((c.volmod & 0x3f) * allvolume) >> 8)) ^ 0x3f));
        else
            lds_setregs((byte)(0x40 + regnum), (byte)(c.volmod ^ 0x3f));
        lds_setregs((byte)(0x60 + regnum), i.mod_ad);
        lds_setregs((byte)(0x80 + regnum), i.mod_sr);
        lds_setregs((byte)(0xe0 + regnum), i.mod_wave);

        // set carrier registers
        lds_setregs((byte)(0x23 + regnum), i.car_misc);
        volcalc = i.car_vol;
        if (c.nextvol == 0)
            c.volcar = volcalc;
        else
            c.volcar = (byte)((volcalc & 0xc0) | (((volcalc & 0x3f) * c.nextvol) >> 6));

        if (allvolume != 0)
            lds_setregs((byte)(0x43 + regnum), (byte)(((c.volcar & 0xc0) | (((c.volcar & 0x3f) * allvolume) >> 8)) ^ 0x3f));
        else
            lds_setregs((byte)(0x43 + regnum), (byte)(c.volcar ^ 0x3f));
        lds_setregs((byte)(0x63 + regnum), i.car_ad);
        lds_setregs((byte)(0x83 + regnum), i.car_sr);
        lds_setregs((byte)(0xe3 + regnum), i.car_wave);
        lds_setregs((byte)(0xc0 + channel_number), i.feedback);
        lds_setregs_adv((byte)(0xb0 + channel_number), 0xdf, 0); // key off

        freq = FREQ(tunehigh);
        octave = (byte)(tunehigh / (12 * 16) - 1);
        if (i.glide == 0)
        {
            if (i.portamento == 0 || c.lasttune == 0)
            {
                lds_setregs((byte)(0xa0 + channel_number), (byte)(freq & 0xff));
                lds_setregs((byte)(0xb0 + channel_number), (byte)((octave << 2) + 0x20 + (freq >> 8)));
                c.lasttune = c.gototune = (ushort)tunehigh;
            }
            else
            {
                c.gototune = (ushort)tunehigh;
                c.portspeed = i.portamento;
                lds_setregs_adv((byte)(0xb0 + channel_number), 0xdf, 0x20); // key on
            }
        }
        else
        {
            lds_setregs((byte)(0xa0 + channel_number), (byte)(freq & 0xff));
            lds_setregs((byte)(0xb0 + channel_number), (byte)((octave << 2) + 0x20 + (freq >> 8)));
            c.lasttune = (ushort)tunehigh;
            c.gototune = (ushort)(tunehigh + ((i.glide + 0x80) & 0xff) - 0x80);
            c.portspeed = i.portamento;
        }

        if (i.vibrato == 0)
            c.vibwait = c.vibspeed = c.vibrate = 0;
        else
        {
            c.vibwait = i.vibdelay;
            c.vibspeed = (byte)((i.vibrato >> 4) + 2);
            c.vibrate = (byte)((i.vibrato & 15) + 1);
        }

        if ((c.trmstay & 0xf0) == 0)
        {
            c.trmwait = (byte)((i.tremwait & 0xf0) >> 3);
            c.trmspeed = (byte)(i.mod_trem >> 4);
            c.trmrate = (byte)(i.mod_trem & 15);
            c.trmcount = 0;
        }

        if ((c.trmstay & 0x0f) == 0)
        {
            c.trcwait = (byte)((i.tremwait & 15) << 1);
            c.trcspeed = (byte)(i.car_trem >> 4);
            c.trcrate = (byte)(i.car_trem & 15);
            c.trccount = 0;
        }

        c.arp_size = (byte)(i.arpeggio & 15);
        c.arp_speed = (byte)(i.arpeggio >> 4);
        Array.Copy(i.arp_tab, c.arp_tab, 12);
        c.keycount = i.keyoff;
        c.nextvol = c.glideto = c.finetune = c.vibcount = c.arp_pos = c.arp_count = 0;
    }
}
