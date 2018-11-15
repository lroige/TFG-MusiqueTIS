#define LOG_SF
using MidiPlayerTK;
using NAudio.Midi;
using NAudio.SoundFont;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace MidiPlayerTK
{
    /// <summary>
    /// SoundFont adapted to Unity
    /// </summary>
    public class SoundFontOptim
    {
        private static string lastDirectory = "";
        /// <summary>
        /// Add a new SoundFont from PC
        /// </summary>
        public static void AddSoundFont()
        {
            try
            {
                string path = EditorUtility.OpenFilePanel("Open and import SoundFont", lastDirectory, "sf2");
                if (!string.IsNullOrEmpty(path))
                {
                    lastDirectory = Path.GetDirectoryName(path);
                    string soundFontName = Path.GetFileNameWithoutExtension(path);
                    string pathSF2Save = Path.Combine(Application.persistentDataPath, MidiPlayerGlobal.PathSF2);
                    if (!Directory.Exists(pathSF2Save))
                        Directory.CreateDirectory(pathSF2Save);
                    pathSF2Save = Path.Combine(pathSF2Save, soundFontName + ".sf2");
                    // Create a copy of the SF2 for future action
                    File.Copy(path, pathSF2Save, true);

                    // Build path to IMSF folder 
                    string imSFPath = Path.Combine(Application.dataPath + "/", MidiPlayerGlobal.PathToSoundfonts);
                    imSFPath = Path.Combine(imSFPath, soundFontName);
                    if (!Directory.Exists(imSFPath))
                        Directory.CreateDirectory(imSFPath);

                    // Load SF2 and build ImSF
                    MidiPlayerGlobal.ImSFCurrent = SoundFontOptim.LoadFromSF2(path);
                    MidiPlayerGlobal.ImSFCurrent.SoundFontName = soundFontName;
                    MidiPlayerGlobal.ImSFCurrent.CreateBankDescription();

                    // Is this sf already exists in MPTK Config ?
                    SoundFontInfo sfi = MidiPlayerGlobal.CurrentMidiSet.SoundFonts.Find(s => s.Name == soundFontName);
                    if (sfi == null)
                    {
                        // Search default bank to select
                        int defaultBankNumber = MidiPlayerGlobal.ImSFCurrent.FirstBank();
                        int drumKitBankNumber = MidiPlayerGlobal.ImSFCurrent.LastBank();
                        // ony one bank found, by default not set drum
                        if (drumKitBankNumber == defaultBankNumber) drumKitBankNumber = -1;

                        sfi = new SoundFontInfo()
                        {
                            Name = soundFontName,
                            DefaultBankNumber = defaultBankNumber,
                            DrumKitBankNumber = drumKitBankNumber,
                            SF2Path = pathSF2Save,
                            //ImSFPath = imSFPath,
                            //ImSFResourcePath = MidiPlayerGlobal.SoundfontsDB + "/" + soundFontName
                        };
                        MidiPlayerGlobal.CurrentMidiSet.SoundFonts.Add(sfi);
                    }
                    MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo = sfi;
                    SoundFontOptim.SaveCurrentIMSF();
                    MidiPlayerGlobal.CurrentMidiSet.Save();
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Optimize selected SoundFont
        /// </summary>
        public static void OptimizeSFFromMidiFiles(BuilderInfo OptimInfo, bool KeepAllPatchs, bool KeepAllZones, bool RemoveUnusedWaves)
        {
            // At least one bank must be selected
            try
            {
                if (MidiPlayerGlobal.ImSFCurrent != null)
                {
                    TPatchUsed filters = CreateFiltersFromMidiList(OptimInfo, KeepAllPatchs, KeepAllZones);
                    if (filters != null)
                    {
                        // Build path to wave
                        string pathToWave = Path.Combine(Application.dataPath + "/", MidiPlayerGlobal.PathToSoundfonts);
                        pathToWave = Path.Combine(pathToWave + "/", MidiPlayerGlobal.ImSFCurrent.SoundFontName);
                        pathToWave = Path.Combine(pathToWave + "/", MidiPlayerGlobal.PathToWave);

                        // Load original SF2 and build ImSF
                        OptimInfo.Add("Reload original SoundFont");
                        List<string> waveSelected = new List<string>();
                        string nameSf = MidiPlayerGlobal.ImSFCurrent.SoundFontName;

                        // Reload original soundfont and convert to the simplified format
                        MidiPlayerGlobal.ImSFCurrent = SoundFontOptim.LoadFromSF2(MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.SF2Path, pathToWave, filters, OptimInfo, waveSelected);
                        MidiPlayerGlobal.ImSFCurrent.SoundFontName = nameSf;

                        // Remove useless wave and calculate stat
                        if (Directory.Exists(pathToWave))
                        {
                            string[] waveFiles = Directory.GetFiles(pathToWave, "*.wav", SearchOption.AllDirectories);
                            // Remove unused wave
                            if (RemoveUnusedWaves)
                                foreach (string waveFile in waveFiles)
                                {
                                    string name = Path.GetFileNameWithoutExtension(waveFile);
                                    bool found = false;
                                    foreach (string selected in waveSelected)
                                    {
                                        if (selected.Contains(name))
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (!found)
                                    {
                                        DeleteResource(waveFile);
                                        Debug.Log("delete " + waveFile);
                                    }
                                }

                            // Calculate size of wave
                            waveFiles = Directory.GetFiles(pathToWave, "*.wav", SearchOption.AllDirectories);
                            long size = 0;
                            foreach (string waveFile in waveFiles)
                                size += new FileInfo(waveFile).Length;
                            MidiPlayerGlobal.ImSFCurrent.WaveSize = size;
                            MidiPlayerGlobal.ImSFCurrent.WaveCount = waveFiles.Length;

                            OptimInfo.Add("Wave saved");
                            OptimInfo.Add("   Count = " + waveFiles.Length);
                            if (size < 1000000)
                                OptimInfo.Add("   Size = " + Math.Round((double)size / 1000d, 2) + " Ko");
                            else
                                OptimInfo.Add("   Size = " + Math.Round((double)size / 1000000d, 2) + " Mo");
                        }
                        else
                            OptimInfo.Add("/!\\ No wave created, midifile will not played /!\\ ");

                        // Add some information to imsf
                        MidiPlayerGlobal.ImSFCurrent.CreateBankDescription();
                        OptimInfo.Add("Save SoundFont");
                        MidiPlayerGlobal.ImSFCurrent.KeepAllPatchs = KeepAllPatchs;
                        MidiPlayerGlobal.ImSFCurrent.KeepAllZones = KeepAllZones;
                        MidiPlayerGlobal.ImSFCurrent.PatchCount = 0;
                        // Calculate count of patch used
                        foreach (ImBank bank in MidiPlayerGlobal.ImSFCurrent.Banks)
                            if (bank != null)
                                foreach (ImPreset preset in bank.Presets)
                                    if (preset != null)
                                        MidiPlayerGlobal.ImSFCurrent.PatchCount++;
                        //Optim.DebugImSf(MidiPlayerGlobal.ImSFCurrent, 0, 48, true, true);
                        // Save ImSf
                        SaveCurrentIMSF();
                        // refresh for new wave
                        AssetDatabase.Refresh();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private static TPatchUsed CreateFiltersFromMidiList(BuilderInfo OptimInfo, bool KeepAllPatchs, bool KeepAllZones)
        {
            //ImSfDebug.SortImSf(MidiPlayerGlobal.ImSFCurrent, -1, -1, true, true);
            //return;
            OptimInfo.Add("Optimize " + MidiPlayerGlobal.ImSFCurrent.SoundFontName);

            TPatchUsed filters;
            if (!KeepAllPatchs)
            {
                OptimInfo.Add("Scan Midifile");
                // Calculate patchs to keep depending patch used by midi files
                filters = MidiOptim.PatchUsed(OptimInfo);
                if (filters == null)
                    return null;
            }
            else
            {
                // Select all patchs 
                filters = new TPatchUsed();
                filters.DefaultBankNumber = MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.DefaultBankNumber;
                filters.DrumKitBankNumber = MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.DrumKitBankNumber;
                if (filters.DefaultBankNumber >= 0 && filters.DefaultBankNumber < filters.BankUsed.Length)
                    filters.BankUsed[filters.DefaultBankNumber] = new TBankUsed();
                if (filters.DrumKitBankNumber >= 0 && filters.DrumKitBankNumber < filters.BankUsed.Length)
                    filters.BankUsed[filters.DrumKitBankNumber] = new TBankUsed();
            }

            // Uncomment for testing purpose
            //int patch = 0;
            //filters.BankUsed[0] = new TBankUsed();
            //filters.BankUsed[0].PatchUsed[patch] = new TNoteUsed();
            //for (int n = 0; n < 128; n++)
            //    filters.BankUsed[0].PatchUsed[patch].Note[n] = 1;

            if (KeepAllPatchs)
            {
                OptimInfo.Add("Keep all patchs");
                for (int b = 0; b < filters.BankUsed.Length; b++)
                {
                    TBankUsed bank = filters.BankUsed[b];
                    if (bank != null)
                    {
                        int patchEnd = bank.PatchUsed.Length - 1;
                        // If drum kit bank, keep only patch 0
                        if (filters.DefaultBankNumber != filters.DrumKitBankNumber && filters.DrumKitBankNumber != -1 && b == filters.DrumKitBankNumber)
                            patchEnd = 0;

                        // For each patch, use all notes
                        for (int p = 0; p <= patchEnd; p++)
                        {
                            if (bank.PatchUsed[p] == null)
                                bank.PatchUsed[p] = new TNoteUsed();
                            for (int n = 0; n < bank.PatchUsed[p].Note.Length; n++)
                                bank.PatchUsed[p].Note[n] = 1;
                        }
                    }
                }
            }

            if (KeepAllZones)
            {
                OptimInfo.Add("Keep all zones (notes and velocities) for selected patch");

                foreach (TBankUsed bank in filters.BankUsed)
                {
                    if (bank != null)
                    {
                        // For each patch, use all notes
                        foreach (TNoteUsed patch in bank.PatchUsed)
                        {
                            if (patch != null)
                                for (int n = 0; n < patch.Note.Length; n++)
                                    patch.Note[n]++;
                        }
                    }
                }
            }

            if (!KeepAllZones)
            {
                OptimInfo.Add("Remove unused patch");

                // Display bank, patch to filters
                int indexBank = 0;
                foreach (TBankUsed bank in filters.BankUsed)
                {
                    if (bank != null)
                    {
                        int indexPatch = 0;
                        foreach (TNoteUsed patch in bank.PatchUsed)
                        {
                            if (patch != null)
                                OptimInfo.Add(string.Format("   Keep bank/patch [{0:000},{1:000}] {2}", indexBank, indexPatch, GetPatchName(indexBank, indexPatch, MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.DrumKitBankNumber)));
                            indexPatch++;
                        }
                    }
                    indexBank++;
                }
            }
            return filters;
        }

        static private void DeleteResource(string filepath)
        {
            try
            {
                Debug.Log("Delete " + filepath);
                File.Delete(filepath);
                // delete also meta
                string meta = filepath + ".meta";
                Debug.Log("Delete " + meta);
                File.Delete(meta);

            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Save current Simplfied
        /// </summary>
        public static void SaveCurrentIMSF()
        {
            try
            {
                MidiPlayerGlobal.ImSFCurrent.DefaultBankNumber = MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.DefaultBankNumber;
                MidiPlayerGlobal.ImSFCurrent.DrumKitBankNumber = MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.DrumKitBankNumber;
                string soundPath = Path.Combine(Application.dataPath + "/", MidiPlayerGlobal.PathToSoundfonts);
                soundPath = Path.Combine(soundPath + "/", MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.Name);
                MidiPlayerGlobal.ImSFCurrent.Save(soundPath, MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.Name);
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        static private string GetPatchName(int ib, int ip, int bankdk)
        {
            if (ib == bankdk)
                return "Drum Kit";
            else
                return PatchChangeEvent.GetPatchName(ip);
        }

        /// <summary>
        /// Create a simplified soundfont from a SF2 file
        /// </summary>
        /// <param name="pathSF"></param>
        /// <param name="wavePath"></param>
        /// <param name="filter "></param>
        /// <param name="info"></param>
        /// <param name="waveSelected"></param>
        /// <param name="addEvenIfNoKeyVel"></param>
        public static ImSoundFont LoadFromSF2(string pathSF, string wavePath = null, TPatchUsed filter = null, BuilderInfo info = null, List<string> waveSelected = null, bool addEvenIfNoKeyVel = false)
        {
            SoundFont soundFont = null;
            ImSoundFont imsf = null;
            CreateWave createwave = null;
            try
            {
                soundFont = new SoundFont(pathSF);
                imsf = new ImSoundFont();
                imsf.SoundFontName = Path.GetFileNameWithoutExtension(pathSF);
                imsf.Banks = new ImBank[ImSoundFont.MAXBANKPRESET];
                createwave = new CreateWave();

            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }

            ImPreset preset;
            ImInstrument instrument;
            ImSample sample;
            try
            {
                List<String> LogInfoCvt = new List<string>();
                LogInfoCvt.Add("SoundFont: '" + imsf.SoundFontName + "'");

                foreach (Preset p in soundFont.Presets)
                {
                    if (filter == null || (filter.BankUsed[p.Bank] != null && filter.BankUsed[p.Bank].PatchUsed[p.PatchNumber] != null))
                    {
                        //if (p.PatchNumber != 18) continue;
                        preset = new ImPreset();
                        preset.Name = p.Name;
                        preset.Bank = p.Bank;
                        preset.Patch = p.PatchNumber;
                        preset.Key = (preset.Bank << 8) + preset.Patch;

                        if (imsf.Banks[preset.Bank] == null)
                        {
                            // New bank, create it
                            imsf.Banks[preset.Bank] = new ImBank()
                            {
                                BankNumber = preset.Bank,
                                Presets = new ImPreset[ImSoundFont.MAXBANKPRESET]
                            };
                        }
                        imsf.Banks[preset.Bank].Presets[preset.Patch] = preset;

                        preset.Instruments = new List<ImInstrument>();

                        LogInfoCvt.Add("Preset:'" + p.Name + "' Bank:" + p.Bank + " PatchNumber:" + p.PatchNumber + " Zones count:" + p.Zones.Count());

                        foreach (Zone zone in p.Zones)
                        {
                            instrument = new ImInstrument();
                            instrument.Samples = new List<ImSample>();
                            instrument.HasKey = false;
                            instrument.HasVel = false;
                            instrument.HasSample = false;

                            LogInfoCvt.Add(string.Format("   Preset.Zones '{2}' Generator:{0} Modulator:{1}", zone.generatorCount, zone.modulatorCount, zone.ToString()));

                            foreach (Modulator modulator in zone.Modulators)
                            {
                                //LogInfoCvt.Add(string.Format("      Preset.Zones.Modulator SourceModulationData:'{0,-21}' SourceTransform:'{1,-6}' SourceModulationAmount:'{2,-21}' Amount:'{3,-5}' DestinationGenerator:'{4}' ",
                                //    modulator.SourceModulationData,
                                //    modulator.SourceTransform,
                                //    modulator.SourceModulationAmount,
                                //    modulator.Amount,
                                //    modulator.DestinationGenerator
                                //    ));

                                LogInfoCvt.Add(string.Format(
                                    "      Preset.Zones.Modulator Amount:'{0}' SourceModulationAmount:'{1:X}' DestinationGenerator:'{2}' SourceModulationData:'{3:X}' SourceTransform:'{4}' ",
                                   modulator.Amount,
                                   modulator.SourceModulationAmount.Raw,
                                   (int)modulator.DestinationGenerator,
                                   modulator.SourceModulationData.Raw,
                                   (int)modulator.SourceTransform
                                   ));
                            }

                            foreach (Generator generator in zone.Generators)
                            {
                                LogInfoCvt.Add(string.Format("      Preset.Zones.Generator:'{0,-41}' LowByte:{1,5} HighByte:{2,5} Int16Amount:{3,5} UInt16Amount:{4,5} Instrument:{5,5}  SampleHeader:{6,5}",
                                    generator.GeneratorType,
                                    generator.LowByteAmount,
                                    generator.HighByteAmount,
                                    generator.Int16Amount,
                                    generator.UInt16Amount,
                                    generator.Instrument,
                                    generator.SampleHeader != null ? generator.SampleHeader.ToString() : ""
                                    ));
                                switch (generator.GeneratorType)
                                {
                                    case GeneratorEnum.KeyRange:
                                        instrument.KeyStart = generator.LowByteAmount;
                                        instrument.KeyEnd = generator.HighByteAmount;
                                        instrument.HasKey = true;

                                        break;
                                    case GeneratorEnum.VelocityRange:
                                        instrument.VelStart = generator.LowByteAmount;
                                        instrument.VelEnd = generator.HighByteAmount;
                                        instrument.HasVel = true;
                                        //LogInfoCvt.Add("      Preset.Zones.Generator.VelRange " + zone.ToString() + " " + gene.ToString());
                                        break;
                                    case GeneratorEnum.Pan:
                                        instrument.Pan = generator.UInt16Amount;
                                        break;
                                    case GeneratorEnum.OverridingRootKey:
                                        instrument.RootKey = generator.UInt16Amount;
                                        break;

                                    case GeneratorEnum.ReverbEffectsSend:
                                        break;

                                    case GeneratorEnum.SampleID:
                                        instrument.HasSample = true;
                                        instrument.SampleName = generator.SampleHeader.SampleName;
                                        break;

                                    case GeneratorEnum.InitialAttenuation:
                                        // P2 = P1  ⋅ 10 pow (cB / 200)
                                        //sample.Attenuation = (float)(Math.Pow(10d, ((double)itgene.UInt16Amount) / -200d));
                                        //LogInfoCvt.Add("      Preset.Zones.Generator.InitialAttenuation " + zone.ToString() + " " + gene.ToString());
                                        break;


                                    case GeneratorEnum.Instrument:
                                        //LogInfoCvt.Add("      Preset.Zones.Generator.Instruments : '" + gene.Instrument.Name + "' zones:" + gene.Instrument.Zones.Count());
                                        bool keepInstrument = true;

                                        if (filter != null && instrument.HasKey)
                                        {
                                            keepInstrument = false;
                                            for (int k = instrument.KeyStart; k <= instrument.KeyEnd; k++)
                                                if (filter.BankUsed[preset.Bank].PatchUsed[preset.Patch].Note[k] > 0)
                                                {
                                                    keepInstrument = true;
                                                    break;
                                                }
                                        }
                                        if (keepInstrument)
                                        {
                                            foreach (Zone itz in generator.Instrument.Zones)
                                            {
                                                LogInfoCvt.Add(string.Format("         Instrument.Zones Generator:{0} Modulator:{1}", itz.generatorCount, itz.modulatorCount));
                                                //Debug.Log("           gene.Instrument.Zones " + itz.ToString());
                                                sample = new ImSample();
                                                sample.HasKey = false;
                                                int OverridingRootKey = -1;
                                                sample.HasVel = false;
                                                sample.WaveFile = null;
                                                sample.KeyStart = -1;
                                                sample.KeyEnd = -1;
                                                sample.IsLoop = false;
                                                sample.Attenuation = 1;
                                                foreach (Modulator itmod in itz.Modulators)
                                                {
                                                    LogInfoCvt.Add(string.Format("            Modulator: SourceModulationData:'{0,-21}' SourceTransform:'{1,-6}' SourceModulationAmount:'{2,-21}' Amount:'{3,-5}' DestinationGenerator:'{4}' ",
                                                        itmod.SourceModulationData,
                                                        itmod.SourceTransform,
                                                        itmod.SourceModulationAmount,
                                                        itmod.Amount,
                                                        itmod.DestinationGenerator
                                                        ));
                                                }

                                                foreach (Generator itgene in itz.Generators)
                                                {
                                                    LogInfoCvt.Add(string.Format("            Generator:'{0,-30}' LowByte:{1,5} HighByte:{2,5} Int16Amount:{3,5} UInt16Amount:{4,5} Instrument:{5,5} SampleHeader:{6,5}",
                                                        itgene.GeneratorType,
                                                        itgene.LowByteAmount,
                                                        itgene.HighByteAmount,
                                                        itgene.Int16Amount,
                                                        itgene.UInt16Amount,
                                                        itgene.Instrument,
                                                        itgene.SampleHeader != null ? itgene.SampleHeader.ToString() : ""
                                                        ));

                                                    //Debug.Log("               gene.Instrument.Zones.Generators " + itgene.ToString());
                                                    switch (itgene.GeneratorType)
                                                    {
                                                        case GeneratorEnum.KeyRange:
                                                            sample.KeyStart = itgene.LowByteAmount;
                                                            sample.KeyEnd = itgene.HighByteAmount;
                                                            sample.HasKey = true;
                                                            //LogInfoCvt.Add(string.Format("                  Preset zone generator type '{0,-25}' KeyStart:{1,5} KeyEnd:{2,5}", itgene.GeneratorType, itgene.LowByteAmount, itgene.HighByteAmount));
                                                            break;
                                                        case GeneratorEnum.VelocityRange:
                                                            sample.VelStart = itgene.LowByteAmount;
                                                            sample.VelEnd = itgene.HighByteAmount;
                                                            sample.HasVel = true;
                                                            //LogInfoCvt.Add(string.Format("                  Preset zone generator type '{0,-25}' VelStart:{1,5} VelEnd:{2,5}", itgene.GeneratorType, itgene.LowByteAmount, itgene.HighByteAmount));
                                                            break;
                                                        case GeneratorEnum.Pan:
                                                            sample.Pan = (short)itgene.UInt16Amount;
                                                            break;
                                                        case GeneratorEnum.OverridingRootKey:
                                                            OverridingRootKey = (short)itgene.UInt16Amount;
                                                            //Debug.Log(string.Format("                  Preset zone generator type '{0,-25}' RootKey:{1,5}", itgene.GeneratorType, OverridingRootKey));
                                                            break;
                                                        case GeneratorEnum.FineTune:
                                                            sample.FineTune = (short)itgene.UInt16Amount;
                                                            //Debug.Log(string.Format("                  Preset zone generator type '{0,-25}' FineTune:{1,5}", itgene.GeneratorType, sample.FineTune));
                                                            break;
                                                        case GeneratorEnum.CoarseTune:
                                                            sample.CoarseTune = (short)itgene.UInt16Amount;
                                                            //Debug.Log(string.Format("                  Preset zone generator type '{0,-25}' CoarseTune:{1,5}", itgene.GeneratorType, sample.CoarseTune));
                                                            break;
                                                        case GeneratorEnum.SampleModes:
                                                            //https://pjb.com.au/midi/sfspec21.html#g54
                                                            if (itgene.UInt16Amount == 1 || itgene.UInt16Amount == 3)
                                                                sample.IsLoop = true;
                                                            else
                                                                sample.IsLoop = false;
                                                            //Debug.Log(string.Format("                  Preset zone generator type '{0,-25}' SampleModes:{1,5}", itgene.GeneratorType, sample.IsLoop));
                                                            break;
                                                        case GeneratorEnum.InitialAttenuation:
                                                            // P2 = P1  ⋅ 10 pow (cB / 200)
                                                            sample.Attenuation = (float)(Math.Pow(10d, ((double)itgene.UInt16Amount) / -200d));
                                                            //LogInfoCvt.Add(string.Format("                  Preset zone generator type '{0,-25}' Attenuation:{1,5} Calculated:{2}", itgene.GeneratorType, (short)itgene.UInt16Amount, sample.Attenuation));
                                                            break;
                                                        case GeneratorEnum.SampleID:
                                                            if (itgene.SampleHeader != null)
                                                            {
                                                                //LogInfoCvt.Add(string.Format("                  Preset zone generator type '{0,-25}' OriginalPitch:{1,5} PitchCorrection:{2,5} '{3}'", itgene.GeneratorType, itgene.SampleHeader.OriginalPitch, itgene.SampleHeader.PitchCorrection, itgene.SampleHeader.SampleName));
                                                                bool keepSample = true;
                                                                //sample.Name = itgene.SampleHeader.SampleName;
                                                                if (sample.KeyStart < 0) sample.KeyStart = 0;
                                                                if (sample.KeyEnd < 0) sample.KeyEnd = 127;
                                                                if (filter != null)
                                                                {
                                                                    keepSample = false;
                                                                    for (int k = sample.KeyStart; k <= sample.KeyEnd; k++)
                                                                        if (filter.BankUsed[preset.Bank].PatchUsed[preset.Patch].Note[k] > 0)
                                                                        {
                                                                            keepSample = true;
                                                                            break;
                                                                        }
                                                                }
                                                                if (keepSample)
                                                                {
                                                                    sample.OriginalPitch = itgene.SampleHeader.OriginalPitch;
                                                                    if (OverridingRootKey >= 0) sample.OriginalPitch = OverridingRootKey;
                                                                    sample.PitchCorrection = itgene.SampleHeader.PitchCorrection;
                                                                    sample.WaveFile = CreateWave.EscapeConvert(itgene.SampleHeader.SampleName) + ".wav";
                                                                    if (wavePath != null)
                                                                    {
                                                                        string filename = System.IO.Path.Combine(wavePath, sample.WaveFile);
                                                                        if (!File.Exists(filename))
                                                                        {
                                                                            if (info != null) info.Add("   Add a new wave " + sample.WaveFile);
                                                                            LogInfoCvt.Add(string.Format("                  Add a new wave '{0}'", sample.WaveFile));
                                                                            createwave.Extract(filename, itgene, soundFont.SampleData);
                                                                        }
                                                                        //else if (info != null) info.Add("   Wave already exists " + sample.WaveFile);

                                                                        //if (waveSelected != null)waveSelected.Add(filename);
                                                                        //Debug.Log(string.Format("                  Sample '{0,20}' RK:{1,3} OP:{2,3} PC{3,3} [{4,5},{5,5}] [{6,5},{7,5}]", sample.Name, OverridingRootKey, sample.OriginalPitch, sample.PitchCorrection, sample.KeyStart, sample.KeyEnd, sample.VelStart, sample.VelEnd));
                                                                    }
                                                                }
                                                                //else if (info != null) info.Add("   Wave not used " + sample.Name);
                                                            }
                                                            break;
                                                    }
                                                }
                                                if (sample.WaveFile != null)
                                                {
                                                    if (!SampleExist(instrument, sample))
                                                    {
                                                        instrument.Samples.Add(sample);
                                                        if (waveSelected != null) waveSelected.Add(sample.WaveFile);
                                                    }
                                                    else
                                                    {
                                                        if (info != null) info.Add("   Sample already exist, discards it " + sample.WaveFile);
                                                        LogInfoCvt.Add(string.Format("   Sample already exist, discards it '{0}'", sample.WaveFile));
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                            if (instrument.Samples.Count > 0)
                            {
                                // Ne peux pas etre utilisé : les range de key et de velocité peuvent etre identiques entre deux instruments mais les key et velocité reelement utilisé peuvent être differents.
                                //if (!InstrumentExist(preset, instrument))
                                //{
                                preset.Instruments.Add(instrument);
                                //Debug.Log("   Add Instrument : '" + p.Name + "' Bank:" + p.Bank + " PatchNumber:" + p.PatchNumber + " Samples:" + instrument.Samples.Count());
                                //                                }
                                //                                else
                                //                                {
                                //                                    if (info != null) info.Add("   Instruments already exist, discards it" + instrument.ToString());
                                //#if LOG_SF
                                //                                    LogInfoCvt.Add(string.Format("   Instruments already exist, discards it '{0}'", instrument.ToString()));
                                //#endif
                                //                                }
                            }
                        }
                    }
                }
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(Path.Combine(Application.persistentDataPath, "Convert " + imsf.SoundFontName + ".txt")))
                    foreach (string line in LogInfoCvt)
                        file.WriteLine(line);
                //NormalizeImSf(imsf);
                //SortImSf(imsf);
                DebugImSf(imsf);

                if (info != null) info.Add("View logs and soundfont detail here : " + Application.persistentDataPath);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return imsf;
        }

        public static bool InstrumentExist(ImPreset preset, ImInstrument instrument)
        {
            bool exist = false;
            try
            {
                foreach (ImInstrument ins in preset.Instruments)
                {
                    if (ins.Samples.Count == instrument.Samples.Count &&
                        ins.KeyStart == instrument.KeyStart && ins.KeyEnd == instrument.KeyEnd &&
                        ins.VelStart == instrument.VelStart && ins.VelEnd == instrument.VelEnd)
                        return true;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return exist;
        }

        /// <summary>
        /// </summary>
        /// <param name="preset"></param>
        /// <param name="instrument"></param>
        /// <returns></returns>
        public static bool InstrumentSamplesAreSames(ImPreset preset, ImInstrument instrument)
        {
            bool exist = false;
            try
            {
                foreach (ImInstrument ins in preset.Instruments)
                {
                    if (SameSamples(ins.Samples, instrument.Samples))
                        return true;

                    //ins.KeyStart == instrument.KeyStart && ins.KeyEnd == instrument.KeyEnd &&
                    //ins.VelStart == instrument.VelStart && ins.VelEnd == instrument.VelEnd)
                    //return true;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return exist;
        }

        private static bool SameSamples(List<ImSample> List1, List<ImSample> List2)
        {
            // return false;
            bool exist = false;
            try
            {
                if (List1.Count != List2.Count)
                    return false;

                for (int i = 0; i < List1.Count; i++)

                    if (List1[i].WaveFile == List2[i].WaveFile)
                        return true;

            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return exist;
        }

        private static bool SampleExist(ImInstrument inst, ImSample sample)
        {
            // return false;
            bool exist = false;
            try
            {
                foreach (ImSample ims in inst.Samples)
                {
                    if (ims.WaveFile == sample.WaveFile &&
                        ims.KeyStart == sample.KeyStart && ims.KeyEnd == sample.KeyEnd &&
                        ims.VelStart == sample.VelStart && ims.VelEnd == sample.VelEnd)
                        return true;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return exist;
        }

        private static void SortImSf(ImSoundFont imsf)
        {
            foreach (ImBank bank in imsf.Banks)
            {
                if (bank != null)
                {
                    foreach (ImPreset p in bank.Presets)
                    {
                        if (p != null)
                        {
                            if (p != null && p.Instruments != null)
                            {
                                foreach (ImInstrument i in p.Instruments)
                                {
                                    i.Samples.Sort((x, y) => x.KeyStart.CompareTo(y.KeyStart));
                                }
                                p.Instruments.Sort((x, y) => x.KeyStart.CompareTo(y.KeyStart));
                            }
                        }
                    }
                }
            }
        }

        private static void NormalizeImSf(ImSoundFont imsf)
        {
            foreach (ImBank bank in imsf.Banks)
            {
                if (bank != null)
                {
                    foreach (ImPreset p in bank.Presets)
                    {
                        if (p != null)
                        {
                            if (p != null && p.Instruments != null)
                            {
                                foreach (ImInstrument i in p.Instruments)
                                {
                                    //if (!i.HasKey)
                                    {
                                        int mink = 999;
                                        int maxk = -999;
                                        foreach (ImSample s in i.Samples)
                                        {
                                            if (s.WaveFile != null)
                                            {
                                                if (s.HasKey)
                                                {
                                                    if (s.KeyStart < mink) mink = s.KeyStart;
                                                    if (s.KeyEnd > maxk) maxk = s.KeyEnd;
                                                }
                                            }
                                        }
                                        i.HasKey = true;

                                        i.KeyStart = mink != 999 ? mink : 0;
                                        i.KeyEnd = maxk != -999 ? maxk : 127;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        static public void DebugImSf(ImSoundFont sf, int ibank = -1, int ipreset = -1, bool showInstrument = true, bool showSample = true)
        {
            try
            {
                List<String> Infos = new List<string>();
                Infos.Add("SoundFont: '" + sf.SoundFontName + "'");
                foreach (ImBank bank in sf.Banks)
                {
                    if (bank != null && (ibank == -1 || bank.BankNumber == ibank))
                    {
                        foreach (ImPreset p in bank.Presets)
                        {
                            if (p != null && (ipreset == -1 || p.Patch == ipreset))
                            {
                                Infos.Add(string.Format("Preset {0:000}:{1:000} {3:000000} {2}", p.Bank, p.Patch, p.Name, p.Key));
                                if (showInstrument)
                                {
                                    foreach (ImInstrument i in p.Instruments)
                                    {
                                        string ikey;
                                        if (i.HasKey)
                                            ikey = string.Format("[{0,5},{1,5}]", i.KeyStart, i.KeyEnd);
                                        else
                                            ikey = "[   No key  ]";

                                        string ivel;
                                        if (i.HasVel)
                                            ivel = string.Format("[{0,5},{1,5}]", i.VelStart, i.VelEnd);
                                        else
                                            ivel = "[   No vel  ]";

                                        Infos.Add(string.Format("  Instrument Key:{0,15} Vel:{1,15} Rk:{2,5} Pan:{3,5} {4} Count:{5}", ikey, ivel, i.RootKey, i.Pan, (i.HasSample) ? i.SampleName : "No sample", i.Samples.Count));
                                        if (showSample)
                                        {
                                            foreach (ImSample s in i.Samples)
                                            {
                                                string skey;
                                                if (s.HasKey)
                                                    skey = string.Format("[{0,5},{1,5}]", s.KeyStart, s.KeyEnd);
                                                else
                                                    skey = "[   No key  ]";

                                                string svel;
                                                if (s.HasVel)
                                                    svel = string.Format("[{0,5},{1,5}]", s.VelStart, s.VelEnd);
                                                else
                                                    svel = "[   No vel  ]";

                                                Infos.Add(string.Format("       Sample Key:{0,15} Vel:{1,15} Rk:{2,1} pitch:{3,2} Corr:{4,3} Pan:{5,4} Loop:{6} Att:{7,3} '{8}'",
                                                    skey, svel, 0, s.OriginalPitch, s.PitchCorrection, s.Pan, s.IsLoop, s.Attenuation, s.WaveFile));
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                string pathSF2Save = Path.Combine(Application.persistentDataPath, "Detail " + sf.SoundFontName + ".txt");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(pathSF2Save))
                    foreach (string line in Infos)
                        file.WriteLine(line);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        static public void SortImSf(ImSoundFont sf, int ibank, int ipreset, bool showInstrument = false, bool showSample = false)
        {
            try
            {
                ImSoundFont nSf = new ImSoundFont();
                nSf.Banks = new ImBank[ImSoundFont.MAXBANKPRESET];
                nSf.SoundFontName = sf.SoundFontName;

                foreach (ImBank bank in sf.Banks)
                {
                    if (bank != null && (ibank == -1 || bank.BankNumber == ibank))
                    {
                        nSf.Banks[bank.BankNumber] = new ImBank();
                        nSf.Banks[bank.BankNumber].BankNumber = bank.BankNumber;
                        nSf.Banks[bank.BankNumber].Presets = new ImPreset[ImSoundFont.MAXBANKPRESET];

                        foreach (ImPreset p in bank.Presets)
                        {
                            if (p != null && (ipreset == -1 || p.Patch == ipreset))
                            {
                                nSf.Banks[bank.BankNumber].Presets[p.Patch] = new ImPreset();
                                nSf.Banks[bank.BankNumber].Presets[p.Patch].Instruments = new List<ImInstrument>();

                                Debug.Log(string.Format("{0:000}:{1:000} {3:000000} {2}", p.Bank, p.Patch, p.Name, p.Key));
                                if (showInstrument)
                                {
                                    p.Instruments = p.Instruments.OrderBy(o => o.KeyStart << 7 + o.VelStart).ToList();
                                    for (int iInstrument = 0; iInstrument < p.Instruments.Count;)
                                    {
                                        ImInstrument instrument = p.Instruments[iInstrument];
                                        string ikey;
                                        if (instrument.HasKey)
                                            ikey = string.Format("[{0,5},{1,5}]", instrument.KeyStart, instrument.KeyEnd);
                                        else
                                            ikey = "[   No key  ]";

                                        string ivel;
                                        if (instrument.HasVel)
                                            ivel = string.Format("[{0,5},{1,5}]", instrument.VelStart, instrument.VelEnd);
                                        else
                                            ivel = "[   No vel  ]";

                                        Debug.Log(string.Format("  Instrument Key:{0,15} Vel:{1,15} Rk:{2,5} Pan:{3,5} {4} CountSample:{5}", ikey, ivel, instrument.RootKey, instrument.Pan, (instrument.HasSample) ? instrument.SampleName : "HasNoWave", instrument.Samples.Count));
                                        if (showSample)
                                        {
                                            instrument.Samples = instrument.Samples.OrderBy(o => o.KeyStart << 7 + o.VelStart).ToList();
                                            for (int iSample = 0; iSample < instrument.Samples.Count;)
                                            {
                                                ImSample sample = instrument.Samples[iSample];
                                                string skey;
                                                if (sample.HasKey)
                                                    skey = string.Format("[{0,5},{1,5}]", sample.KeyStart, sample.KeyEnd);
                                                else
                                                    skey = "[   No key  ]";

                                                string svel;
                                                if (sample.HasVel)
                                                    svel = string.Format("[{0,5},{1,5}]", sample.VelStart, sample.VelEnd);
                                                else
                                                    svel = "[   No vel  ]";

                                                Debug.Log(string.Format("       SampleKey:{0,15} Vel:{1,15} Rk:{2,5} pitch:{3,5} Corr:{4,5} Pan:{5,6} '{6}'", skey, svel, 0, sample.OriginalPitch, sample.PitchCorrection, sample.Pan, sample.WaveFile));
                                                iSample++;
                                            }
                                        }
                                        iInstrument++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Keep only default bank and drumkit bank
        /// </summary>
        static public void OptimizeBanks(ImSoundFont sf)
        {
            for (int ibank = 0; ibank < sf.Banks.Length; ibank++)
                if (ibank != sf.DefaultBankNumber && ibank != sf.DrumKitBankNumber)
                    sf.Banks[ibank] = null;
            SaveCurrentIMSF();
        }

        //public void OptimizePreset(List<int> presetsToKeep)
        //{
        //    OptimizeBanks();
        //    if (imsf.DefaultBankNumber > -1)
        //    {
        //        for (int ipreset = 0; ipreset < ImSoundFont.MAXBANKPRESET; ipreset++)
        //            if (!presetsToKeep.Contains(ipreset))
        //                imsf.Banks[imsf.DefaultBankNumber].Presets[ipreset] = null;
        //    }

        //    for (int ibank = 0; ibank < imsf.Banks.Length; ibank++)
        //        if (ibank != imsf.DefaultBankNumber && ibank != imsf.DrumKitBankNumber)
        //            imsf.Banks[ibank] = null;
        //}


        /// <summary>
        /// Load an ImSoundFont from a desktop file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ImSoundFont Load(string path, string name)
        {
            ImSoundFont loaded = null;
            try
            {
                string Filepath = Path.Combine(path, name);
                Filepath += MidiPlayerGlobal.ExtensionSoundFileFile;

                if (File.Exists(Filepath))
                {
                    var serializer = new XmlSerializer(typeof(ImSoundFont));
                    using (var stream = new FileStream(Filepath, FileMode.Open))
                    {
                        loaded = serializer.Deserialize(stream) as ImSoundFont;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return loaded;
        }


        /// <summary>
        /// Add infoormation about preset
        /// </summary>
        //public void CreateBankDescription()
        //{
        //    foreach (ImBank bank in imsf.Banks)
        //        if (bank != null)
        //        {
        //            bank.Description = "";
        //            foreach (ImPreset preset in bank.Presets)
        //                if (preset != null)
        //                    bank.Description += preset.Name + " ; ";
        //        }
        //}
    }

    public class PatchFilter
    {

    }
}
