
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System;
using UnityEngine.Events;
using System.Net;

namespace MidiPlayerTK
{
    /// <summary>
    /// Script for the prefab MidiExternalPlayer. 
    /// Play a midi file from a local deskop or from a web site
    /// </summary>
    public class MidiExternalPlayer : MidiFilePlayer
    {
        /// <summary>
        /// Path to Midi name to play.
        /// <para>Can be a local file or an URI to a web Midi, but mandatory:</para>
        ///    <para>for a local file prefix path with file: file://</para>
        ///    <para>for a web resource prefix url with http:// or https://</para>
        /// </summary>
        public override string MPTK_MidiName { get { return pathmidiNameToPlay; } set { pathmidiNameToPlay = value; } }
        [SerializeField]
        [HideInInspector]
        private string pathmidiNameToPlay;


        /// <summary>
        /// Index Midi to play or playing. 
        /// return -1 if not found
        /// </summary>
        /// <param name="index"></param>
        public override int MPTK_MidiIndex
        {
            get
            {
                Debug.LogWarning("MPTK_Next not available for MidiExternalPlayer");
                return -1;
            }
            set
            {
                Debug.LogWarning("MPTK_Next not available for MidiExternalPlayer");
            }
        }

        protected override void Awake()
        {
            //Debug.Log("Awake MidiExternalPlayer:" + MPTK_IsPlaying);
            base.Awake();
        }

        protected override void Start()
        {
            //Debug.Log("Start MidiExternalPlayer:" + MPTK_IsPlaying);
            try
            {
                if (MPTK_PlayOnStart)
                {
                    StartCoroutine(TheadPlayIfReady());
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            base.Start();
        }

        /// <summary>
        /// Play the midi file defined in MPTK_MidiName
        /// </summary>
        public override void MPTK_Play()
        {
            try
            {
                if (MidiPlayerGlobal.SoundFontLoaded)
                {
                    playPause = false;
                    if (!MPTK_IsPlaying)
                    {
                        if (string.IsNullOrEmpty(pathmidiNameToPlay))
                            Debug.LogWarning("MPTK_MidiName not defined");
                        else if (!pathmidiNameToPlay.ToLower().StartsWith("file://") &&
                                 !pathmidiNameToPlay.ToLower().StartsWith("http://") &&
                                 !pathmidiNameToPlay.ToLower().StartsWith("https://"))
                            Debug.LogWarning("MPTK_MidiName must start with file:// or http:// or https:// - " + pathmidiNameToPlay);
                        else
                            StartCoroutine(TheadLoadDataAndPlay());
                    }
                    else
                        Debug.LogWarning("Already playing - " + pathmidiNameToPlay);
                }
            }

            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private IEnumerator TheadLoadDataAndPlay()
        {
            //for TU
            //pathmidiNameToPlay = @"C:\Users\Thierry\Desktop\BIM\Sound\Midi\Bach The Art of Fugue - No1.mid";
            //pathmidiNameToPlay = "http://www.midishrine.com/midipp/ngc/Animal_Crossing/kk_ballad.mid";
            //pathmidiNameToPlay = "http://www.midiworld.com/download/4000";
            //pathmidiNameToPlay = "http://www.midiworld.com/midis/other/bach/bwv1060b.mid";
            //http://www.midishrine.com/midipp/n64/Zelda_64_-_The_Ocarina_of_Time/kakariko.mid

            yield return StartCoroutine(MPTK_ClearAllSound(true));
            //Debug.Log("Load " + pathmidiNameToPlay);
            // Asynchrone loading of the midi file
            using (WWW www = new WWW(pathmidiNameToPlay))
            {
                yield return www;
                if (www.bytes != null && www.bytes.Length > 4 && System.Text.Encoding.Default.GetString(www.bytes, 0, 4) == "MThd")
                    // Start playing
                    StartCoroutine(ThreadPlay(www.bytes));
                else
                    Debug.LogWarning("Midi not find or not a Midi file - " + pathmidiNameToPlay);
            }
        }

        // Not used, unity WWW is better !
        private class WebClient : System.Net.WebClient
        {
            public int Timeout { get; set; }

            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest lWebRequest = base.GetWebRequest(uri);
                lWebRequest.Timeout = Timeout;
                ((HttpWebRequest)lWebRequest).ReadWriteTimeout = Timeout;
                return lWebRequest;
            }
        }

        private bool IsUri(string path)
        {
            Uri uri;
            return Uri.TryCreate(path, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        // Not used ...
        //private bool IsUrlExist(string url)
        //{
        //    bool exist = true;
        //    try
        //    {
        //        WebRequest req = WebRequest.Create(url);
        //        req.Timeout = 5000; //ms
        //        WebResponse res = req.GetResponse();
        //    }
        //    catch (WebException ex)
        //    {
        //        Debug.Log(ex.Message);
        //        exist = false;
        //    }
        //    return exist;
        //}

        /// <summary>
        /// Play next Midi in list - no sense with no list
        /// </summary>
        public override void MPTK_Next()
        {
            try
            {
                Debug.LogWarning("MPTK_Next not available for MidiExternalPlayer");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Play previous Midi in list - no sense with no list
        /// </summary>
        public override void MPTK_Previous()
        {
            try
            {
                Debug.LogWarning("MPTK_Next not available for MidiExternalPlayer");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
    }
}

