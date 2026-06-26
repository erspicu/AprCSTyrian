namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/opl.c —— DOSBox OPL2 FM 合成模擬器（ADLIBEMU.C, Ken Silverman / DOSBox Team）。
/// 僅移植 OPL2 路徑（9 通道，無 OPL3 4op/stereo）。指標改為陣列索引 / ref。
/// </summary>
internal static class Opl
{
    private const int NUM_CHANNELS = 9;
    private const int MAXOPERATORS = NUM_CHANNELS * 2;

    private const double PI = 3.1415926535897932384626433832795;

    private const int FIXEDPT = 0x10000;       // 16+16
    private const int FIXEDPT_LFO = 0x1000000; // 8+24
    private const int WAVEPREC = 1024;
    private const double INTFREQU = 14318180.0 / 288.0;

    private const int OF_TYPE_ATT = 0;
    private const int OF_TYPE_DEC = 1;
    private const int OF_TYPE_REL = 2;
    private const int OF_TYPE_SUS = 3;
    private const int OF_TYPE_SUS_NOKEEP = 4;
    private const int OF_TYPE_OFF = 5;

    private const int ARC_CONTROL = 0x00;
    private const int ARC_TVS_KSR_MUL = 0x20;
    private const int ARC_KSL_OUTLEV = 0x40;
    private const int ARC_ATTR_DECR = 0x60;
    private const int ARC_SUSL_RELR = 0x80;
    private const int ARC_FREQ_NUM = 0xa0;
    private const int ARC_KON_BNUM = 0xb0;
    private const int ARC_PERC_MODE = 0xbd;
    private const int ARC_FEEDBACK = 0xc0;
    private const int ARC_WAVE_SEL = 0xe0;

    private const int OP_ACT_OFF = 0x00;
    private const int OP_ACT_NORMAL = 0x01;
    private const int OP_ACT_PERC = 0x02;

    private const int BLOCKBUF_SIZE = 512;

    private const int VIBTAB_SIZE = 8;
    private const int TREMTAB_SIZE = 53;
    private const double TREM_FREQ = 3.7;

    private struct op_type
    {
        public int cval, lastcval;          // current/last output (feedback)
        public uint tcount, wfpos, tinc;    // waveform position/time increment
        public double amp, step_amp;        // amplification (envelope)
        public double vol;
        public double sustain_level;
        public int mfbi;                    // feedback amount
        public double a0, a1, a2, a3;       // attack rate coefficients
        public double decaymul, releasemul;
        public uint op_state;
        public uint toff;
        public int freq_high;
        public int cur_wform;               // offset into wavtable (取代 Bit16s* cur_wform)
        public uint cur_wmask;
        public uint act_state;
        public bool sus_keep;
        public bool vibrato, tremolo;

        public uint generator_pos;
        public long cur_env_step;
        public long env_step_a, env_step_d, env_step_r;
        public byte step_skip_pos_a;
        public long env_step_skip_a;
    }

    private static readonly op_type[] op = new op_type[MAXOPERATORS];

    private static long int_samplerate;
    private static byte status;
    private static readonly byte[] adlibreg = new byte[256];
    private static readonly byte[] wave_sel = new byte[22];

    private static uint vibtab_pos, vibtab_add, tremtab_pos, tremtab_add;

    private static uint generator_add;
    private static double recipsamp;
    private static readonly short[] wavtable = new short[WAVEPREC * 3];

    private static readonly int[] vib_table = new int[VIBTAB_SIZE];
    private static readonly int[] trem_table = new int[TREMTAB_SIZE * 2];

    private static readonly int[] vibval_const = new int[BLOCKBUF_SIZE];
    private static readonly int[] tremval_const = new int[BLOCKBUF_SIZE];
    private static readonly int[] vibval_var1 = new int[BLOCKBUF_SIZE];
    private static readonly int[] vibval_var2 = new int[BLOCKBUF_SIZE];

    private static readonly double[] kslmul = { 0.0, 0.5, 0.25, 1.0 };
    private static readonly double[] frqmul_tab = { 0.5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 12, 12, 15, 15 };
    private static readonly double[] frqmul = new double[16];
    private static readonly byte[,] kslev = new byte[8, 16];

    private static readonly byte[] modulatorbase = { 0, 1, 2, 8, 9, 10, 16, 17, 18 };
    private static readonly byte[] regbase2modop = { 0, 1, 2, 0, 1, 2, 0, 0, 3, 4, 5, 3, 4, 5, 0, 0, 6, 7, 8, 6, 7, 8 };
    private static readonly byte[] regbase2op = { 0, 1, 2, 9, 10, 11, 0, 0, 3, 4, 5, 12, 13, 14, 0, 0, 6, 7, 8, 15, 16, 17 };

    private static readonly uint[] waveform = { WAVEPREC, WAVEPREC >> 1, WAVEPREC, (WAVEPREC * 3) >> 2, 0, 0, (WAVEPREC * 5) >> 2, WAVEPREC << 1 };
    private static readonly uint[] wavemask = { WAVEPREC - 1, WAVEPREC - 1, (WAVEPREC >> 1) - 1, (WAVEPREC >> 1) - 1, WAVEPREC - 1, ((WAVEPREC * 3) >> 2) - 1, WAVEPREC >> 1, WAVEPREC - 1 };
    private static readonly uint[] wavestart = { 0, WAVEPREC >> 1, 0, WAVEPREC >> 2, 0, 0, 0, WAVEPREC >> 3 };

    private static readonly double[] attackconst = { 1 / 2.82624, 1 / 2.25280, 1 / 1.88416, 1 / 1.59744 };
    private static readonly double[] decrelconst = { 1 / 39.28064, 1 / 31.41608, 1 / 26.17344, 1 / 22.44608 };

    private static uint _rng = 1;
    private static int rand() { _rng = _rng * 1103515245u + 12345u; return (int)((_rng >> 16) & 0x7fff); }

    private static bool initfirstime = false;

    // ===== opl.h 介面 =====
    public static void opl_init() => adlib_init((uint)Loudness.audioSampleRate);
    public static void opl_write(int reg, byte val) => adlib_write(reg, val);
    public static unsafe void opl_update(short* buf, int num) => adlib_getsample(buf, num);

    // ===== operator advance/output =====
    private static void operator_advance(ref op_type op_pt, int vib)
    {
        op_pt.wfpos = op_pt.tcount;
        op_pt.tcount += op_pt.tinc;
        op_pt.tcount += (uint)((int)op_pt.tinc * vib / FIXEDPT);
        op_pt.generator_pos += generator_add;
    }

    private static void operator_advance_drums(ref op_type op_pt1, int vib1, ref op_type op_pt2, int vib2, ref op_type op_pt3, int vib3)
    {
        uint c1 = op_pt1.tcount / FIXEDPT;
        uint c3 = op_pt3.tcount / FIXEDPT;
        uint phasebit = (((c1 & 0x88) ^ ((c1 << 5) & 0x80)) | ((c3 ^ (c3 << 2)) & 0x20)) != 0 ? 0x02u : 0x00u;

        uint noisebit = (uint)(rand() & 1);
        uint snare_phase_bit = ((op_pt1.tcount / FIXEDPT) / 0x100) & 1;

        // Hihat
        uint inttm = (phasebit << 8) | (0x34u << (int)(phasebit ^ (noisebit << 1)));
        op_pt1.wfpos = inttm * FIXEDPT;
        op_pt1.tcount += op_pt1.tinc;
        op_pt1.tcount += (uint)((int)op_pt1.tinc * vib1 / FIXEDPT);
        op_pt1.generator_pos += generator_add;

        // Snare
        inttm = ((1 + snare_phase_bit) ^ noisebit) << 8;
        op_pt2.wfpos = inttm * FIXEDPT;
        op_pt2.tcount += op_pt2.tinc;
        op_pt2.tcount += (uint)((int)op_pt2.tinc * vib2 / FIXEDPT);
        op_pt2.generator_pos += generator_add;

        // Cymbal
        inttm = (1 + phasebit) << 8;
        op_pt3.wfpos = inttm * FIXEDPT;
        op_pt3.tcount += op_pt3.tinc;
        op_pt3.tcount += (uint)((int)op_pt3.tinc * vib3 / FIXEDPT);
        op_pt3.generator_pos += generator_add;
    }

    private static void operator_output(ref op_type op_pt, int modulator, int trem)
    {
        if (op_pt.op_state != OF_TYPE_OFF)
        {
            op_pt.lastcval = op_pt.cval;
            uint i = (op_pt.wfpos + (uint)modulator) / FIXEDPT;
            op_pt.cval = (int)(op_pt.step_amp * op_pt.vol * wavtable[op_pt.cur_wform + (int)(i & op_pt.cur_wmask)] * trem / 16.0);
        }
    }

    private static void operator_sustain(ref op_type op_pt)
    {
        uint num_steps_add = op_pt.generator_pos / FIXEDPT;
        for (uint ct = 0; ct < num_steps_add; ct++)
            op_pt.cur_env_step++;
        op_pt.generator_pos -= num_steps_add * FIXEDPT;
    }

    private static void operator_release(ref op_type op_pt)
    {
        if (op_pt.amp > 0.00000001)
            op_pt.amp *= op_pt.releasemul;

        uint num_steps_add = op_pt.generator_pos / FIXEDPT;
        for (uint ct = 0; ct < num_steps_add; ct++)
        {
            op_pt.cur_env_step++;
            if ((op_pt.cur_env_step & op_pt.env_step_r) == 0)
            {
                if (op_pt.amp <= 0.00000001)
                {
                    op_pt.amp = 0.0;
                    if (op_pt.op_state == OF_TYPE_REL)
                        op_pt.op_state = OF_TYPE_OFF;
                }
                op_pt.step_amp = op_pt.amp;
            }
        }
        op_pt.generator_pos -= num_steps_add * FIXEDPT;
    }

    private static void operator_decay(ref op_type op_pt)
    {
        if (op_pt.amp > op_pt.sustain_level)
            op_pt.amp *= op_pt.decaymul;

        uint num_steps_add = op_pt.generator_pos / FIXEDPT;
        for (uint ct = 0; ct < num_steps_add; ct++)
        {
            op_pt.cur_env_step++;
            if ((op_pt.cur_env_step & op_pt.env_step_d) == 0)
            {
                if (op_pt.amp <= op_pt.sustain_level)
                {
                    if (op_pt.sus_keep)
                    {
                        op_pt.op_state = OF_TYPE_SUS;
                        op_pt.amp = op_pt.sustain_level;
                    }
                    else
                    {
                        op_pt.op_state = OF_TYPE_SUS_NOKEEP;
                    }
                }
                op_pt.step_amp = op_pt.amp;
            }
        }
        op_pt.generator_pos -= num_steps_add * FIXEDPT;
    }

    private static void operator_attack(ref op_type op_pt)
    {
        op_pt.amp = ((op_pt.a3 * op_pt.amp + op_pt.a2) * op_pt.amp + op_pt.a1) * op_pt.amp + op_pt.a0;

        uint num_steps_add = op_pt.generator_pos / FIXEDPT;
        for (uint ct = 0; ct < num_steps_add; ct++)
        {
            op_pt.cur_env_step++;
            if ((op_pt.cur_env_step & op_pt.env_step_a) == 0)
            {
                if (op_pt.amp > 1.0)
                {
                    op_pt.op_state = OF_TYPE_DEC;
                    op_pt.amp = 1.0;
                    op_pt.step_amp = 1.0;
                }
                op_pt.step_skip_pos_a <<= 1;
                if (op_pt.step_skip_pos_a == 0) op_pt.step_skip_pos_a = 1;
                if ((op_pt.step_skip_pos_a & op_pt.env_step_skip_a) != 0)
                    op_pt.step_amp = op_pt.amp;
            }
        }
        op_pt.generator_pos -= num_steps_add * FIXEDPT;
    }

    private static void op_func(ref op_type o)
    {
        switch (o.op_state)
        {
            case OF_TYPE_ATT: operator_attack(ref o); break;
            case OF_TYPE_DEC: operator_decay(ref o); break;
            case OF_TYPE_REL: operator_release(ref o); break;
            case OF_TYPE_SUS: operator_sustain(ref o); break;
            case OF_TYPE_SUS_NOKEEP: operator_release(ref o); break;
            // OF_TYPE_OFF: operator_off (no-op)
        }
    }

    // ===== change_* =====
    private static void change_attackrate(int regbase, ref op_type op_pt)
    {
        int attackrate = adlibreg[ARC_ATTR_DECR + regbase] >> 4;
        if (attackrate != 0)
        {
            double f = Math.Pow(2.0, attackrate + (op_pt.toff >> 2) - 1) * attackconst[op_pt.toff & 3] * recipsamp;
            op_pt.a0 = 0.0377 * f;
            op_pt.a1 = 10.73 * f + 1;
            op_pt.a2 = -17.57 * f;
            op_pt.a3 = 7.42 * f;

            long step_skip = attackrate * 4 + (long)op_pt.toff;
            long steps = step_skip >> 2;
            op_pt.env_step_a = (1 << (int)(steps <= 12 ? 12 - steps : 0)) - 1;

            int step_num = (step_skip <= 48) ? (int)(4 - (step_skip & 3)) : 0;
            byte[] step_skip_mask = { 0xff, 0xfe, 0xee, 0xba, 0xaa };
            op_pt.env_step_skip_a = step_skip_mask[step_num];

            if (step_skip >= 62)
            {
                op_pt.a0 = 2.0;
                op_pt.a1 = 0.0;
                op_pt.a2 = 0.0;
                op_pt.a3 = 0.0;
            }
        }
        else
        {
            op_pt.a0 = 0.0;
            op_pt.a1 = 1.0;
            op_pt.a2 = 0.0;
            op_pt.a3 = 0.0;
            op_pt.env_step_a = 0;
            op_pt.env_step_skip_a = 0;
        }
    }

    private static void change_decayrate(int regbase, ref op_type op_pt)
    {
        int decayrate = adlibreg[ARC_ATTR_DECR + regbase] & 15;
        if (decayrate != 0)
        {
            double f = -7.4493 * decrelconst[op_pt.toff & 3] * recipsamp;
            op_pt.decaymul = Math.Pow(2.0, f * Math.Pow(2.0, (double)(decayrate + (op_pt.toff >> 2))));
            long steps = (decayrate * 4 + (long)op_pt.toff) >> 2;
            op_pt.env_step_d = (1 << (int)(steps <= 12 ? 12 - steps : 0)) - 1;
        }
        else
        {
            op_pt.decaymul = 1.0;
            op_pt.env_step_d = 0;
        }
    }

    private static void change_releaserate(int regbase, ref op_type op_pt)
    {
        int releaserate = adlibreg[ARC_SUSL_RELR + regbase] & 15;
        if (releaserate != 0)
        {
            double f = -7.4493 * decrelconst[op_pt.toff & 3] * recipsamp;
            op_pt.releasemul = Math.Pow(2.0, f * Math.Pow(2.0, (double)(releaserate + (op_pt.toff >> 2))));
            long steps = (releaserate * 4 + (long)op_pt.toff) >> 2;
            op_pt.env_step_r = (1 << (int)(steps <= 12 ? 12 - steps : 0)) - 1;
        }
        else
        {
            op_pt.releasemul = 1.0;
            op_pt.env_step_r = 0;
        }
    }

    private static void change_sustainlevel(int regbase, ref op_type op_pt)
    {
        int sustainlevel = adlibreg[ARC_SUSL_RELR + regbase] >> 4;
        if (sustainlevel < 15)
            op_pt.sustain_level = Math.Pow(2.0, sustainlevel * -0.5);
        else
            op_pt.sustain_level = 0.0;
    }

    private static void change_waveform(int regbase, ref op_type op_pt)
    {
        op_pt.cur_wmask = wavemask[wave_sel[regbase]];
        op_pt.cur_wform = (int)waveform[wave_sel[regbase]];
    }

    private static void change_keepsustain(int regbase, ref op_type op_pt)
    {
        op_pt.sus_keep = (adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x20) > 0;
        if (op_pt.op_state == OF_TYPE_SUS)
        {
            if (!op_pt.sus_keep) op_pt.op_state = OF_TYPE_SUS_NOKEEP;
        }
        else if (op_pt.op_state == OF_TYPE_SUS_NOKEEP)
        {
            if (op_pt.sus_keep) op_pt.op_state = OF_TYPE_SUS;
        }
    }

    private static void change_vibrato(int regbase, ref op_type op_pt)
    {
        op_pt.vibrato = (adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x40) != 0;
        op_pt.tremolo = (adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x80) != 0;
    }

    private static void change_feedback(int chanbase, ref op_type op_pt)
    {
        int feedback = adlibreg[ARC_FEEDBACK + chanbase] & 14;
        if (feedback != 0) op_pt.mfbi = (int)Math.Pow(2.0, (double)((feedback >> 1) + 8));
        else op_pt.mfbi = 0;
    }

    private static void change_frequency(int chanbase, int regbase, ref op_type op_pt)
    {
        uint frn = (((uint)adlibreg[ARC_KON_BNUM + chanbase] & 3) << 8) + adlibreg[ARC_FREQ_NUM + chanbase];
        uint oct = ((uint)adlibreg[ARC_KON_BNUM + chanbase] >> 2) & 7;
        op_pt.freq_high = (int)((frn >> 7) & 7);

        uint note_sel = (uint)(adlibreg[8] >> 6) & 1;
        op_pt.toff = ((frn >> 9) & (note_sel ^ 1)) | ((frn >> 8) & note_sel);
        op_pt.toff += (oct << 1);

        if ((adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x10) == 0) op_pt.toff >>= 2;

        op_pt.tinc = (uint)((double)(frn << (int)oct) * frqmul[adlibreg[ARC_TVS_KSR_MUL + regbase] & 15]);

        double vol_in = (adlibreg[ARC_KSL_OUTLEV + regbase] & 63) +
                        kslmul[adlibreg[ARC_KSL_OUTLEV + regbase] >> 6] * kslev[oct, frn >> 6];
        op_pt.vol = Math.Pow(2.0, vol_in * -0.125 - 14);

        change_attackrate(regbase, ref op_pt);
        change_decayrate(regbase, ref op_pt);
        change_releaserate(regbase, ref op_pt);
    }

    private static void enable_operator(int regbase, ref op_type op_pt, uint act_type)
    {
        if (op_pt.act_state == OP_ACT_OFF)
        {
            int wselbase = regbase;
            op_pt.tcount = wavestart[wave_sel[wselbase]] * FIXEDPT;
            op_pt.op_state = OF_TYPE_ATT;
            op_pt.act_state |= act_type;
        }
    }

    private static void disable_operator(ref op_type op_pt, uint act_type)
    {
        if (op_pt.act_state != OP_ACT_OFF)
        {
            op_pt.act_state &= ~act_type;
            if (op_pt.act_state == OP_ACT_OFF)
            {
                if (op_pt.op_state != OF_TYPE_OFF) op_pt.op_state = OF_TYPE_REL;
            }
        }
    }

    public static void adlib_init(uint samplerate)
    {
        int_samplerate = samplerate;
        generator_add = (uint)(INTFREQU * FIXEDPT / int_samplerate);

        Array.Clear(adlibreg, 0, adlibreg.Length);
        Array.Clear(op, 0, op.Length);
        Array.Clear(wave_sel, 0, wave_sel.Length);

        for (int i = 0; i < MAXOPERATORS; i++)
        {
            op[i].op_state = OF_TYPE_OFF;
            op[i].act_state = OP_ACT_OFF;
            op[i].amp = 0.0;
            op[i].step_amp = 0.0;
            op[i].vol = 0.0;
            op[i].tcount = 0;
            op[i].tinc = 0;
            op[i].toff = 0;
            op[i].cur_wmask = wavemask[0];
            op[i].cur_wform = (int)waveform[0];
            op[i].freq_high = 0;
            op[i].generator_pos = 0;
            op[i].cur_env_step = 0;
            op[i].env_step_a = 0;
            op[i].env_step_d = 0;
            op[i].env_step_r = 0;
            op[i].step_skip_pos_a = 0;
            op[i].env_step_skip_a = 0;
        }

        recipsamp = 1.0 / int_samplerate;
        for (int i = 15; i >= 0; i--)
            frqmul[i] = frqmul_tab[i] * INTFREQU / WAVEPREC * FIXEDPT * recipsamp;

        status = 0;

        vib_table[0] = 8;
        vib_table[1] = 4;
        vib_table[2] = 0;
        vib_table[3] = -4;
        for (int i = 4; i < VIBTAB_SIZE; i++) vib_table[i] = vib_table[i - 4] * -1;

        vibtab_add = (uint)((long)VIBTAB_SIZE * FIXEDPT_LFO / 8192 * (long)INTFREQU / int_samplerate);
        vibtab_pos = 0;
        for (int i = 0; i < BLOCKBUF_SIZE; i++) vibval_const[i] = 0;

        int[] trem_table_int = new int[TREMTAB_SIZE];
        for (int i = 0; i < 14; i++) trem_table_int[i] = i - 13;
        for (int i = 14; i < 41; i++) trem_table_int[i] = -i + 14;
        for (int i = 41; i < 53; i++) trem_table_int[i] = i - 40 - 26;

        for (int i = 0; i < TREMTAB_SIZE; i++)
        {
            double trem_val1 = trem_table_int[i] * 4.8 / 26.0 / 6.0;
            double trem_val2 = (trem_table_int[i] / 4) * 1.2 / 6.0 / 6.0;
            trem_table[i] = (int)(Math.Pow(2.0, trem_val1) * FIXEDPT);
            trem_table[TREMTAB_SIZE + i] = (int)(Math.Pow(2.0, trem_val2) * FIXEDPT);
        }

        tremtab_add = (uint)((double)TREMTAB_SIZE * TREM_FREQ * FIXEDPT_LFO / int_samplerate);
        tremtab_pos = 0;
        for (int i = 0; i < BLOCKBUF_SIZE; i++) tremval_const[i] = FIXEDPT;

        if (!initfirstime)
        {
            initfirstime = true;

            for (int i = 0; i < (WAVEPREC >> 1); i++)
            {
                wavtable[(i << 1) + WAVEPREC] = (short)(16384 * Math.Sin((i << 1) * PI * 2 / WAVEPREC));
                wavtable[(i << 1) + 1 + WAVEPREC] = (short)(16384 * Math.Sin(((i << 1) + 1) * PI * 2 / WAVEPREC));
                wavtable[i] = wavtable[(i << 1) + WAVEPREC];
            }
            for (int i = 0; i < (WAVEPREC >> 3); i++)
            {
                wavtable[i + (WAVEPREC << 1)] = (short)(wavtable[i + (WAVEPREC >> 3)] - 16384);
                wavtable[i + ((WAVEPREC * 17) >> 3)] = (short)(wavtable[i + (WAVEPREC >> 2)] + 16384);
            }

            kslev[7, 0] = 0; kslev[7, 1] = 24; kslev[7, 2] = 32; kslev[7, 3] = 37;
            kslev[7, 4] = 40; kslev[7, 5] = 43; kslev[7, 6] = 45; kslev[7, 7] = 47;
            kslev[7, 8] = 48;
            for (int i = 9; i < 16; i++) kslev[7, i] = (byte)(i + 41);
            for (int j = 6; j >= 0; j--)
            {
                for (int i = 0; i < 16; i++)
                {
                    int oct = kslev[j + 1, i] - 8;
                    if (oct < 0) oct = 0;
                    kslev[j, i] = (byte)oct;
                }
            }
        }
    }

    public static void adlib_write(int idx, byte val)
    {
        adlibreg[idx] = val;

        switch (idx & 0xf0)
        {
        case ARC_CONTROL:
            switch (idx)
            {
            case 0x04:
                if ((val & 0x80) != 0) status &= unchecked((byte)~0x60);
                else status = 0;
                break;
            }
            break;
        case ARC_TVS_KSR_MUL:
        case ARC_TVS_KSR_MUL + 0x10:
        {
            int num = idx & 7;
            int bas = (idx - ARC_TVS_KSR_MUL) & 0xff;
            if (num < 6 && bas < 22)
            {
                int modop = regbase2modop[bas];
                int chanbase = modop;
                int oi = modop + ((num < 3) ? 0 : 9);
                change_keepsustain(bas, ref op[oi]);
                change_vibrato(bas, ref op[oi]);
                change_frequency(chanbase, bas, ref op[oi]);
            }
        }
        break;
        case ARC_KSL_OUTLEV:
        case ARC_KSL_OUTLEV + 0x10:
        {
            int num = idx & 7;
            int bas = (idx - ARC_KSL_OUTLEV) & 0xff;
            if (num < 6 && bas < 22)
            {
                int modop = regbase2modop[bas];
                int chanbase = modop;
                int oi = modop + ((num < 3) ? 0 : 9);
                change_frequency(chanbase, bas, ref op[oi]);
            }
        }
        break;
        case ARC_ATTR_DECR:
        case ARC_ATTR_DECR + 0x10:
        {
            int num = idx & 7;
            int bas = (idx - ARC_ATTR_DECR) & 0xff;
            if (num < 6 && bas < 22)
            {
                int oi = regbase2op[bas];
                change_attackrate(bas, ref op[oi]);
                change_decayrate(bas, ref op[oi]);
            }
        }
        break;
        case ARC_SUSL_RELR:
        case ARC_SUSL_RELR + 0x10:
        {
            int num = idx & 7;
            int bas = (idx - ARC_SUSL_RELR) & 0xff;
            if (num < 6 && bas < 22)
            {
                int oi = regbase2op[bas];
                change_releaserate(bas, ref op[oi]);
                change_sustainlevel(bas, ref op[oi]);
            }
        }
        break;
        case ARC_FREQ_NUM:
        {
            int bas = (idx - ARC_FREQ_NUM) & 0xff;
            if (bas < 9)
            {
                int opbase = bas;
                int modbase = modulatorbase[bas];
                int chanbase = bas;
                change_frequency(chanbase, modbase, ref op[opbase]);
                change_frequency(chanbase, modbase + 3, ref op[opbase + 9]);
            }
        }
        break;
        case ARC_KON_BNUM:
        {
            if (idx == ARC_PERC_MODE)
            {
                if ((val & 0x30) == 0x30)
                {
                    enable_operator(16, ref op[6], OP_ACT_PERC); change_frequency(6, 16, ref op[6]);
                    enable_operator(16 + 3, ref op[6 + 9], OP_ACT_PERC); change_frequency(6, 16 + 3, ref op[6 + 9]);
                }
                else { disable_operator(ref op[6], OP_ACT_PERC); disable_operator(ref op[6 + 9], OP_ACT_PERC); }
                if ((val & 0x28) == 0x28) { enable_operator(17 + 3, ref op[16], OP_ACT_PERC); change_frequency(7, 17 + 3, ref op[16]); }
                else disable_operator(ref op[16], OP_ACT_PERC);
                if ((val & 0x24) == 0x24) { enable_operator(18, ref op[8], OP_ACT_PERC); change_frequency(8, 18, ref op[8]); }
                else disable_operator(ref op[8], OP_ACT_PERC);
                if ((val & 0x22) == 0x22) { enable_operator(18 + 3, ref op[8 + 9], OP_ACT_PERC); change_frequency(8, 18 + 3, ref op[8 + 9]); }
                else disable_operator(ref op[8 + 9], OP_ACT_PERC);
                if ((val & 0x21) == 0x21) { enable_operator(17, ref op[7], OP_ACT_PERC); change_frequency(7, 17, ref op[7]); }
                else disable_operator(ref op[7], OP_ACT_PERC);
                break;
            }

            int bas = (idx - ARC_KON_BNUM) & 0xff;
            if (bas < 9)
            {
                int opbase = bas;
                int modbase = modulatorbase[bas];
                if ((val & 32) != 0)
                {
                    enable_operator(modbase, ref op[opbase], OP_ACT_NORMAL);
                    enable_operator(modbase + 3, ref op[opbase + 9], OP_ACT_NORMAL);
                }
                else
                {
                    disable_operator(ref op[opbase], OP_ACT_NORMAL);
                    disable_operator(ref op[opbase + 9], OP_ACT_NORMAL);
                }
                int chanbase = bas;
                change_frequency(chanbase, modbase, ref op[opbase]);
                change_frequency(chanbase, modbase + 3, ref op[opbase + 9]);
            }
        }
        break;
        case ARC_FEEDBACK:
        {
            int bas = (idx - ARC_FEEDBACK) & 0xff;
            if (bas < 9)
            {
                int opbase = bas;
                int chanbase = bas;
                change_feedback(chanbase, ref op[opbase]);
            }
        }
        break;
        case ARC_WAVE_SEL:
        case ARC_WAVE_SEL + 0x10:
        {
            int num = idx & 7;
            int bas = (idx - ARC_WAVE_SEL) & 0xff;
            if (num < 6 && bas < 22)
            {
                if ((adlibreg[0x01] & 0x20) != 0)
                {
                    wave_sel[bas] = (byte)(val & 3);
                    int oi = regbase2modop[bas] + ((num < 3) ? 0 : 9);
                    change_waveform(bas, ref op[oi]);
                }
            }
        }
        break;
        }
    }

    private static short clipit16(int ival)
    {
        if (ival < 32768)
        {
            if (ival > -32769) return (short)ival;
            return -32768;
        }
        return 32767;
    }

    public static unsafe void adlib_getsample(short* sndptr, int numsamples)
    {
        int[] outbufl = new int[BLOCKBUF_SIZE];
        int[] vib_lut = new int[BLOCKBUF_SIZE];
        int[] trem_lut = new int[BLOCKBUF_SIZE];

        int samples_to_process = numsamples;

        for (int cursmp = 0; cursmp < samples_to_process; )
        {
            int endsamples = samples_to_process - cursmp;
            if (endsamples > BLOCKBUF_SIZE) endsamples = BLOCKBUF_SIZE;

            Array.Clear(outbufl, 0, endsamples);

            int[] vibval1, vibval2, vibval3, vibval4;
            int[] tremval1, tremval2, tremval3, tremval4;

            int vib_tshift = ((adlibreg[ARC_PERC_MODE] & 0x40) == 0) ? 1 : 0;
            for (int i = 0; i < endsamples; i++)
            {
                vibtab_pos += vibtab_add;
                if (vibtab_pos / FIXEDPT_LFO >= VIBTAB_SIZE) vibtab_pos -= VIBTAB_SIZE * FIXEDPT_LFO;
                vib_lut[i] = vib_table[vibtab_pos / FIXEDPT_LFO] >> vib_tshift;

                tremtab_pos += tremtab_add;
                if (tremtab_pos / FIXEDPT_LFO >= TREMTAB_SIZE) tremtab_pos -= TREMTAB_SIZE * FIXEDPT_LFO;
                if ((adlibreg[ARC_PERC_MODE] & 0x80) != 0) trem_lut[i] = trem_table[tremtab_pos / FIXEDPT_LFO];
                else trem_lut[i] = trem_table[TREMTAB_SIZE + tremtab_pos / FIXEDPT_LFO];
            }

            if ((adlibreg[ARC_PERC_MODE] & 0x20) != 0)
            {
                // BassDrum (channel 6: op[6] modulator, op[15] carrier)
                if ((adlibreg[ARC_FEEDBACK + 6] & 1) != 0)
                {
                    if (op[15].op_state != OF_TYPE_OFF)
                    {
                        if (op[15].vibrato) { vibval1 = vibval_var1; for (int i = 0; i < endsamples; i++) vibval1[i] = vib_lut[i] * op[15].freq_high / 8 * FIXEDPT * 70 / 50000; }
                        else vibval1 = vibval_const;
                        tremval1 = op[15].tremolo ? trem_lut : tremval_const;
                        for (int i = 0; i < endsamples; i++)
                        {
                            operator_advance(ref op[15], vibval1[i]);
                            op_func(ref op[15]);
                            operator_output(ref op[15], 0, tremval1[i]);
                            outbufl[i] += op[15].cval * 2;
                        }
                    }
                }
                else
                {
                    if (op[15].op_state != OF_TYPE_OFF || op[6].op_state != OF_TYPE_OFF)
                    {
                        if (op[6].vibrato && op[6].op_state != OF_TYPE_OFF) { vibval1 = vibval_var1; for (int i = 0; i < endsamples; i++) vibval1[i] = vib_lut[i] * op[6].freq_high / 8 * FIXEDPT * 70 / 50000; }
                        else vibval1 = vibval_const;
                        if (op[15].vibrato && op[15].op_state != OF_TYPE_OFF) { vibval2 = vibval_var2; for (int i = 0; i < endsamples; i++) vibval2[i] = vib_lut[i] * op[15].freq_high / 8 * FIXEDPT * 70 / 50000; }
                        else vibval2 = vibval_const;
                        tremval1 = op[6].tremolo ? trem_lut : tremval_const;
                        tremval2 = op[15].tremolo ? trem_lut : tremval_const;
                        for (int i = 0; i < endsamples; i++)
                        {
                            operator_advance(ref op[6], vibval1[i]);
                            op_func(ref op[6]);
                            operator_output(ref op[6], (op[6].lastcval + op[6].cval) * op[6].mfbi / 2, tremval1[i]);
                            operator_advance(ref op[15], vibval2[i]);
                            op_func(ref op[15]);
                            operator_output(ref op[15], op[6].cval * FIXEDPT, tremval2[i]);
                            outbufl[i] += op[15].cval * 2;
                        }
                    }
                }

                // TomTom (op[8])
                if (op[8].op_state != OF_TYPE_OFF)
                {
                    if (op[8].vibrato) { vibval3 = vibval_var1; for (int i = 0; i < endsamples; i++) vibval3[i] = vib_lut[i] * op[8].freq_high / 8 * FIXEDPT * 70 / 50000; }
                    else vibval3 = vibval_const;
                    tremval3 = op[8].tremolo ? trem_lut : tremval_const;
                    for (int i = 0; i < endsamples; i++)
                    {
                        operator_advance(ref op[8], vibval3[i]);
                        op_func(ref op[8]);
                        operator_output(ref op[8], 0, tremval3[i]);
                        outbufl[i] += op[8].cval * 2;
                    }
                }

                // Snare/Hihat (op[7],op[16]), Cymbal (op[17])
                if (op[7].op_state != OF_TYPE_OFF || op[16].op_state != OF_TYPE_OFF || op[17].op_state != OF_TYPE_OFF)
                {
                    if (op[7].vibrato && op[7].op_state != OF_TYPE_OFF) { vibval1 = vibval_var1; for (int i = 0; i < endsamples; i++) vibval1[i] = vib_lut[i] * op[7].freq_high / 8 * FIXEDPT * 70 / 50000; }
                    else vibval1 = vibval_const;
                    if (op[16].vibrato && op[16].op_state == OF_TYPE_OFF) { vibval2 = vibval_var2; for (int i = 0; i < endsamples; i++) vibval2[i] = vib_lut[i] * op[16].freq_high / 8 * FIXEDPT * 70 / 50000; }
                    else vibval2 = vibval_const;
                    tremval1 = op[7].tremolo ? trem_lut : tremval_const;
                    tremval2 = op[16].tremolo ? trem_lut : tremval_const;

                    if (op[17].vibrato && op[17].op_state == OF_TYPE_OFF) { vibval4 = vibval_var2; for (int i = 0; i < endsamples; i++) vibval4[i] = vib_lut[i] * op[17].freq_high / 8 * FIXEDPT * 70 / 50000; }
                    else vibval4 = vibval_const;
                    tremval4 = op[17].tremolo ? trem_lut : tremval_const;

                    for (int i = 0; i < endsamples; i++)
                    {
                        operator_advance_drums(ref op[7], vibval1[i], ref op[16], vibval2[i], ref op[17], vibval4[i]);
                        op_func(ref op[7]); operator_output(ref op[7], 0, tremval1[i]);
                        op_func(ref op[16]); operator_output(ref op[16], 0, tremval2[i]);
                        op_func(ref op[17]); operator_output(ref op[17], 0, tremval4[i]);
                        outbufl[i] += (op[7].cval + op[16].cval + op[17].cval) * 2;
                    }
                }
            }

            int max_channel = NUM_CHANNELS;
            for (int cur_ch = max_channel - 1; cur_ch >= 0; cur_ch--)
            {
                if ((adlibreg[ARC_PERC_MODE] & 0x20) != 0 && cur_ch >= 6 && cur_ch < 9) continue;

                int k = cur_ch;
                int c0 = cur_ch;       // cptr[0]
                int c9 = cur_ch + 9;   // cptr[9]

                if ((adlibreg[ARC_FEEDBACK + k] & 1) != 0)
                {
                    // 2op additive synthesis
                    if (op[c9].op_state == OF_TYPE_OFF && op[c0].op_state == OF_TYPE_OFF) continue;
                    if (op[c0].vibrato && op[c0].op_state != OF_TYPE_OFF) { vibval1 = vibval_var1; for (int i = 0; i < endsamples; i++) vibval1[i] = vib_lut[i] * op[c0].freq_high / 8 * FIXEDPT * 70 / 50000; }
                    else vibval1 = vibval_const;
                    if (op[c9].vibrato && op[c9].op_state != OF_TYPE_OFF) { vibval2 = vibval_var2; for (int i = 0; i < endsamples; i++) vibval2[i] = vib_lut[i] * op[c9].freq_high / 8 * FIXEDPT * 70 / 50000; }
                    else vibval2 = vibval_const;
                    tremval1 = op[c0].tremolo ? trem_lut : tremval_const;
                    tremval2 = op[c9].tremolo ? trem_lut : tremval_const;

                    for (int i = 0; i < endsamples; i++)
                    {
                        operator_advance(ref op[c0], vibval1[i]);
                        op_func(ref op[c0]);
                        operator_output(ref op[c0], (op[c0].lastcval + op[c0].cval) * op[c0].mfbi / 2, tremval1[i]);

                        operator_advance(ref op[c9], vibval2[i]);
                        op_func(ref op[c9]);
                        operator_output(ref op[c9], 0, tremval2[i]);

                        outbufl[i] += op[c9].cval + op[c0].cval;
                    }
                }
                else
                {
                    // 2op frequency modulation
                    if (op[c9].op_state == OF_TYPE_OFF && op[c0].op_state == OF_TYPE_OFF) continue;
                    if (op[c0].vibrato && op[c0].op_state != OF_TYPE_OFF) { vibval1 = vibval_var1; for (int i = 0; i < endsamples; i++) vibval1[i] = vib_lut[i] * op[c0].freq_high / 8 * FIXEDPT * 70 / 50000; }
                    else vibval1 = vibval_const;
                    if (op[c9].vibrato && op[c9].op_state != OF_TYPE_OFF) { vibval2 = vibval_var2; for (int i = 0; i < endsamples; i++) vibval2[i] = vib_lut[i] * op[c9].freq_high / 8 * FIXEDPT * 70 / 50000; }
                    else vibval2 = vibval_const;
                    tremval1 = op[c0].tremolo ? trem_lut : tremval_const;
                    tremval2 = op[c9].tremolo ? trem_lut : tremval_const;

                    for (int i = 0; i < endsamples; i++)
                    {
                        operator_advance(ref op[c0], vibval1[i]);
                        op_func(ref op[c0]);
                        operator_output(ref op[c0], (op[c0].lastcval + op[c0].cval) * op[c0].mfbi / 2, tremval1[i]);

                        operator_advance(ref op[c9], vibval2[i]);
                        op_func(ref op[c9]);
                        operator_output(ref op[c9], op[c0].cval * FIXEDPT, tremval2[i]);

                        outbufl[i] += op[c9].cval;
                    }
                }
            }

            for (int i = 0; i < endsamples; i++)
                *sndptr++ = clipit16(outbufl[i]);

            cursmp += endsamples;
        }
    }
}
