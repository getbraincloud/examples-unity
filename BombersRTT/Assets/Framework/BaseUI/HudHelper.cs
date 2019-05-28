using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

namespace Gameframework
{
    class HudHelper
    {

        // TODO:: refactor into Generic Message SubState
        #region GenericMessageSubState 
        public static void DisplayMessageDialog(string title, string message, string buttonTxt, GenericMessageSubState.OnDialogAction onCloseAction = null)
        {
            if (GStateManager.Instance.NextSubStateId != GenericMessageSubState.STATE_NAME)
            {
                GStateManager.InitializeDelegate init = null;
                init = (BaseState state) =>
                {
                    GenericMessageSubState messageSubState = state as GenericMessageSubState;
                    if (messageSubState)
                    {
                        GStateManager.Instance.OnInitializeDelegate -= init;

                        Canvas canvas = state.GetComponentInChildren<Canvas>();
                        canvas.sortingOrder = GLOBAL_MSG_SORTING_ORDER;
                        if (messageSubState != null)
                        {
                            messageSubState.LateInit(title, message, onCloseAction, buttonTxt);
                        }
                    }
                };

                GStateManager.Instance.OnInitializeDelegate += init;
                GStateManager.Instance.PushSubState(GenericMessageSubState.STATE_NAME);
            }

        }

        public static void DisplayMessageDialogTwoButton(string title, string message, string in_leftButtonTxt,
            GenericMessageSubState.OnDialogAction onLeftAction = null, string in_rightButtonTxt = "", GenericMessageSubState.OnDialogAction onRightAction = null,
            GenericMessageSubState.eButtonColors mainColor = GenericMessageSubState.eButtonColors.GREEN, GenericMessageSubState.eButtonColors secondColor = GenericMessageSubState.eButtonColors.GREEN,
            bool in_showCloseButton = false, GenericMessageSubState.OnDialogAction onCloseAction = null)
        {
            GStateManager.InitializeDelegate init = null;
            init = (BaseState state) =>
            {
                GStateManager.Instance.OnInitializeDelegate -= init;
                var messageSubState = state as GenericMessageSubState;
                Canvas canvas = state.GetComponentInChildren<Canvas>();
                canvas.sortingOrder = GLOBAL_MSG_SORTING_ORDER;
                messageSubState.LateInit(title, message, onLeftAction, in_leftButtonTxt, onRightAction, in_rightButtonTxt, in_showCloseButton, onCloseAction);
                messageSubState.SetButtonColors(mainColor, secondColor);
            };

            GStateManager.Instance.OnInitializeDelegate += init;
            GStateManager.Instance.PushSubState(GenericMessageSubState.STATE_NAME);
        }

        public static void DisplayMessageDialogTwoButtonWithInfoBox(string title, string message, string in_InfoBoxTxt, string in_leftButtonTxt,
            GenericMessageSubState.OnDialogAction onLeftAction = null, string in_rightButtonTxt = "", GenericMessageSubState.OnDialogAction onRightAction = null,
            GenericMessageSubState.eButtonColors mainColor = GenericMessageSubState.eButtonColors.GREEN, GenericMessageSubState.eButtonColors secondColor = GenericMessageSubState.eButtonColors.GREEN,
            bool in_showCloseButton = false, GenericMessageSubState.OnDialogAction onCloseAction = null)
        {
            GStateManager.InitializeDelegate init = null;
            init = (BaseState state) =>
            {
                GStateManager.Instance.OnInitializeDelegate -= init;
                var messageSubState = state as GenericMessageSubState;
                Canvas canvas = state.GetComponentInChildren<Canvas>();
                canvas.sortingOrder = GLOBAL_MSG_SORTING_ORDER;
                messageSubState.LateInit(title, message, onLeftAction, in_leftButtonTxt, onRightAction, in_rightButtonTxt, in_showCloseButton, onCloseAction);
                messageSubState.SetInfoBox(in_InfoBoxTxt);
                messageSubState.SetButtonColors(mainColor, secondColor);
            };

            GStateManager.Instance.OnInitializeDelegate += init;
            GStateManager.Instance.PushSubState(GenericMessageSubState.STATE_NAME);
        }

        public static void DisplayInvalidPasswordDialog(GenericMessageSubState.OnDialogAction onAction = null)
        {
            DisplayMessageDialog("PASSWORD DOES NOT MATCH", "THE PASSWORD ASSOCIATED WITH THIS ACCOUNT DOES NOT MATCH. PLEASE TRY AGAIN.", "OK", onAction);
        }
        #endregion

        #region Public Consts
        public const int GLOBAL_MSG_SORTING_ORDER = 30001;

        public const string GOOD_COLOR = "<color=#00FF00FF>";
        public const string BAD_COLOR = "<color=#FF0000FF>";
        public const string WHITE_COLOR = "<color=#FFFFFFFF>";

        public static char[] NUMBER_ARRAY = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        public static float PREFERRED_HEIGHT = 768.0f;
        public static float PREFERRED_WIDTH = 1024.0f;

        public const string MatchEmailPattern =
            @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
            + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
              + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
            + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";

        #endregion

        #region String Manipulation
        // updates a string reference so that the tailing value has an extra space in it
        // if less then double digits
        public static void updateDisplayStringEvenNumbers(int in_value, ref string in_modifiedString)
        {
            if (in_value < 10 && in_value > -10)
            {
                in_modifiedString += "  ";
            }
        }

        public static string ToGUIString(int in_val)
        {
            return string.Format("{0:#,###0}", in_val);
        }

        public static string ToGUIString(ulong in_val)
        {
            return string.Format("{0:#,###0}", in_val);
        }

        public static string ToGUIString(long in_val)
        {
            return string.Format("{0:#,###0}", in_val);
        }

        public static string ToGUIString(float in_val)
        {
            return string.Format("{0:#,###0}", in_val);
        }

        public static string ToMinuteSecondString(int in_value)
        {
            TimeSpan span = TimeSpan.FromSeconds(in_value);
            return string.Format("{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);
        }

        public static string ToOrdinal(long in_value)
        {
            const string TH = "th";
            string s = in_value.ToString();

            // Negative and zero have no ordinal representation
            if (in_value < 1)
            {
                return s;
            }

            in_value %= 100;
            if ((in_value >= 11) && (in_value <= 13))
            {
                return s + TH;
            }

            switch (in_value % 10)
            {
                case 1: return s + "st";
                case 2: return s + "nd";
                case 3: return s + "rd";
                default: return s + TH;
            }
        }

        public static string ToOrdinal(int in_value)
        {
            return ToOrdinal((long)in_value);
        }
        #endregion

        #region Color Manipulation
        public static Color IntToColor(int color)
        {
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color >> 0);
            return new Color(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
        }

        public static int ColorToInt(Color color)
        {
            byte r = (byte)(color.r * 255);
            byte g = (byte)(color.g * 255);
            byte b = (byte)(color.b * 255);
            int toReturn = (int)((r << 16) | (g << 8) | (b << 0));
            return toReturn;
        }

        public static string ColorToHex(Color color)
        {
            Color32 c = color;
            var hex = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", c.r, c.g, c.b, c.a);
            return hex;
        }
        #endregion

        #region Position Manipulation
        public static float GetYPos(float in_float)
        {
            return (Screen.height / PREFERRED_HEIGHT) * in_float;
        }

        public static float GetXPos(float in_float)
        {
            return (Screen.width / PREFERRED_WIDTH) * in_float;
        }
        #endregion

        #region Quick Match Functions
        public static float QuickAbs(float in_f)
        {
            return in_f >= 0 ? in_f : in_f * -1.0f;
        }

        public static double QuickAbs(double in_d)
        {
            return in_d >= 0 ? in_d : in_d * -1.0;
        }

        public static int QuickAbs(int in_i)
        {
            return in_i >= 0 ? in_i : in_i * -1;
        }

        public static int QuickRound(float in_f)
        {
            return in_f >= 0 ? (int)(in_f + 0.5f) : (int)(in_f - 0.5f);
        }

        public static double QuickIntPower(double in_dBase, int in_iPower)
        {
            double dToReturn = 1.0f;
            while (in_iPower > 0)
            {
                if (in_iPower % 2 == 1)
                    dToReturn *= in_dBase;
                in_iPower >>= 1;
                in_dBase *= in_dBase;
            }
            return dToReturn;
        }
        #endregion

        #region Date Time Helpers
        public static int GetCurrentYear()
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            Calendar cal = dfi.Calendar;
            DateTime date = BrainCloud.Util.BcTimeToDateTime((long)GPlayerMgr.Instance.CurrentServerTime);
            return cal.GetYear(date);
        }

        public static int GetCurrentWeekOfTheYear()
        {
            DateTime date = BrainCloud.Util.BcTimeToDateTime((long)GPlayerMgr.Instance.CurrentServerTime);
            return GetWeekOfTheYear(date);
        }

        public static int GetWeekOfTheYear(DateTime in_date)
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            Calendar cal = dfi.Calendar;


            return cal.GetWeekOfYear(in_date, dfi.CalendarWeekRule,
                                                dfi.FirstDayOfWeek);
        }
        #endregion

        public static void OpenURL(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {

#if UNITY_WEBGL && !UNITY_EDITOR
                Application.ExternalEval("window.open('" + url + "');");
#else
                Application.OpenURL(url);
#endif
            }
        }
        public static byte[] StructureToByteArray<T>(T str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];
            GCHandle h = default(GCHandle);
            try
            {
                h = GCHandle.Alloc(arr, GCHandleType.Pinned);
                Marshal.StructureToPtr<T>(str, h.AddrOfPinnedObject(), false);
            }
            finally
            {
                if (h.IsAllocated)
                {
                    h.Free();
                }
            }

            return arr;
        }

        public static T ByteArrayToStructure<T>(byte[] arr) where T : struct
        {
            T str = default(T);
            if (arr.Length != Marshal.SizeOf(str)) throw new InvalidOperationException("WRONG SIZE STRUCTURE COPY");
            GCHandle h = default(GCHandle);
            try
            {
                h = GCHandle.Alloc(arr, GCHandleType.Pinned);
                str = Marshal.PtrToStructure<T>(h.AddrOfPinnedObject());
            }
            finally
            {
                if (h.IsAllocated)
                {
                    h.Free();
                }
            }

            return str;
        }
       
        public static void UpdateTextLabelWithValues(Text in_textLabel, string in_update)
        {
            if (in_textLabel)
            {
                in_textLabel.text = in_update;
            }
        }

        public static void UpdateProgressBarWithValues(Image in_image, long in_value, long in_max)
        {
            if (in_image)
            {
                in_image.fillAmount = GetFillAmount((float)in_value, (float)in_max);
            }
        }

        public static float GetFillAmount(float in_current, float in_max)
        {
            return in_current / in_max;
        }

        public static bool AlmostEquals(double double1, double double2, double precision)
        {
            return (QuickAbs(double1 - double2) <= precision);
        }

        public static bool AlmostEquals(float double1, float double2, float precision)
        {
            return (QuickAbs(double1 - double2) <= precision);
        }

        public static short ToShort(byte byte1, byte byte2)
        {
            return (short)((byte2 << 8) | (byte1 << 0));
        }

        public static void FromShort(short number, out byte byte1, out byte byte2)
        {
            byte2 = (byte)(number >> 8);
            byte1 = (byte)(number >> 0);
        }

        public static void ConvertUnixTimeToInts(uint unixTime, ref int secs, ref int mins, ref int hours, ref int days)
        {
            secs = Mathf.FloorToInt(unixTime / 1000);
            mins = Mathf.FloorToInt(secs / 60);
            hours = Mathf.FloorToInt(mins / 60);
            days = Mathf.FloorToInt(hours / 24);
            secs %= 60;
            mins %= 60;
            hours %= 24;
        }


        /// <summary>
        /// This returns time as a string formatted like this: "3d 13h 17m 40s" 
        /// <para>It will also ignore the highest digits if they are zero. Ex: "0d 0h 13m 20s" will only return "13m 20s"</para>
        /// <para>in_onlyTwoTimeValues = true, will return at most two time values, so "3d 13h", instead of the more precise "3d 13h 17m 40s"</para>
        /// </summary>
        /// <param name="unixTime"></param>
        /// <param name="in_twoValuesForNumbers"></param>
        /// <param name="in_onlyTwoTimeValues"></param>
        /// <returns></returns>
        public static string ConvertUnixTimeToGUIString(uint unixTime, bool in_twoValuesForNumbers = true, int in_maxDisplayUnits = 4)
        {
            int secs = 0;
            int mins = 0;
            int hours = 0;
            int days = 0;

            int numAccumulated = 0;
            string time = "";
            ConvertUnixTimeToInts(unixTime, ref secs, ref mins, ref hours, ref days);
            // days
            if (days > 0)
            {
                time = days.ToString() + "d ";
                ++numAccumulated;
            }
            // hours
            if (hours > 0 && numAccumulated < in_maxDisplayUnits)
            {
                time = time + (in_twoValuesForNumbers && (hours < 10) ? "0" + hours.ToString() : hours.ToString()) + "h ";
                ++numAccumulated;
            }

            // mins
            if (mins > 0 && numAccumulated < in_maxDisplayUnits)
            {
                time = time + (in_twoValuesForNumbers && (mins < 10) ? "0" + mins.ToString() : mins.ToString()) + "m ";
                ++numAccumulated;
            }
            // secs
            if (secs >= 0 && numAccumulated < in_maxDisplayUnits)
            {
                time = time + (in_twoValuesForNumbers && (secs < 10) ? "0" + secs.ToString() : secs.ToString()) + "s";
                ++numAccumulated;
            }

            return time;
        }

        public static string ConvertUnixTimeToGUIString(int unixTime)
        {
            return ConvertUnixTimeToGUIString((uint)unixTime);
        }

        public static bool IsEmailFormat(string in_email)
        {
            if (in_email != null)
                return Regex.IsMatch(in_email, MatchEmailPattern);
            else
                return false;
        }

        public static char ValidateInput(string text, int charIndex, char addedChar)
        {
            try
            {
                string toString = "";
                toString += addedChar;

                string[] parts = Regex.Split(toString, @"[^a-zA-Z \d]");    //Accepts only A-Z a-z 0-9 and spaces
                if (parts.Length > 0 && parts[0].Length > 0)
                    return parts[0][0];
                else
                    return '\0';
            }
            catch (System.Exception ex)
            {
                GDebug.LogError(ex.ToString());
                return '\0';
            }
        }

        public static long GetLongValue(object in_obj)
        {
            long toReturn = 0;
            if (in_obj != null)
            {
                try
                {
                    toReturn = (long)in_obj;
                }
                catch (InvalidCastException)
                {
                    try
                    {
                        toReturn = (long)(int)in_obj;
                    }
                    catch (InvalidCastException)
                    {
                        toReturn = long.Parse((string)in_obj);
                    }
                }
            }
            return toReturn;
        }

        public static long GetLongValue(IDictionary<string, object> dict, string in_key)
        {
            long toReturn = 0;
            if (dict != null && dict.ContainsKey(in_key))
            {
                toReturn = GetLongValue(dict[in_key]);
            }
            return toReturn;
        }

        // Extracts a short and long description from a Json string otherwise it returns the input string
        public static void ParseJsonDescription(string in_description, ref string out_shortDescription, ref string out_longDescription)
        {
            if (in_description.Contains("shortDescription"))
            {
                Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_description);
                out_shortDescription = jsonMessage["shortDescription"] as string;
                out_longDescription = jsonMessage["longDescription"] as string;
            }
            else
            {
                out_shortDescription = in_description;
                out_longDescription = in_description;
            }
        }
    }
}
