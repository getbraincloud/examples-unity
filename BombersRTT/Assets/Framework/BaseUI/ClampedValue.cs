using System;
using UnityEngine;

namespace Gameframework
{
    [Serializable]
    public class ClampedFloat
    {
        #region Public Accessors
        public float Value
        {
            get { return m_Value; }
            set { m_Value = value; clampValue(); }
        }
        public float Minimum
        {
            get { return m_Minimum; }
            set { m_Minimum = Mathf.Min(value, m_Maximum); clampValue(); }
        }
        public float Maximum
        {
            get { return m_Maximum; }
            set { m_Maximum = Mathf.Max(value, m_Minimum); clampValue(); }
        }
        #endregion

        #region Constructors
        public ClampedFloat(float in_fValue)
        {
            Value = in_fValue;
            Minimum = 0.0f;
            Maximum = in_fValue;
        }

        public ClampedFloat(float in_fValue, float in_fMinimum, float in_fMaximum)
        {
            Value = in_fValue;
            Minimum = in_fMinimum;
            Maximum = in_fMaximum;
        }
        #endregion

        #region Private
        private void clampValue()
        {
            if (m_Value < m_Minimum)
                m_Value = m_Minimum;
            if (m_Value > m_Maximum)
                m_Value = m_Maximum;
        }

        [SerializeField]
        private float m_Value;
        [SerializeField]
        private float m_Minimum;
        [SerializeField]
        private float m_Maximum;
        #endregion

        #region Operator Overloading
        public static float operator +(ClampedFloat f1, ClampedFloat f2) { return f1.Value + f2.Value; }
        public static float operator -(ClampedFloat f1, ClampedFloat f2) { return f1.Value - f2.Value; }
        public static float operator *(ClampedFloat f1, ClampedFloat f2) { return f1.Value * f2.Value; }
        public static float operator /(ClampedFloat f1, ClampedFloat f2) { return f1.Value / f2.Value; }
        public static float operator +(ClampedFloat f1, float f2) { return f1.Value + f2; }
        public static float operator -(ClampedFloat f1, float f2) { return f1.Value - f2; }
        public static float operator *(ClampedFloat f1, float f2) { return f1.Value * f2; }
        public static float operator /(ClampedFloat f1, float f2) { return f1.Value / f2; }
        public static implicit operator ClampedFloat(float f) { return new ClampedFloat(f); }
        public static implicit operator float(ClampedFloat f) { return f.Value; }
        public static bool operator ==(ClampedFloat f1, ClampedFloat f2) { return f1.Value == f2.Value; }
        public static bool operator !=(ClampedFloat f1, ClampedFloat f2) { return f1.Value != f2.Value; }
        public static bool operator ==(ClampedFloat f1, float f2) { return f1.Value == f2; }
        public static bool operator !=(ClampedFloat f1, float f2) { return f1.Value != f2; }
        public static implicit operator string(ClampedFloat f) { return f.Value.ToString(); }
        public override bool Equals(object obj)
        {
            try
            {
                return (bool)(this == (ClampedFloat)obj);
            }
            catch
            {
                try
                {
                    return (bool)(this == (float)obj);
                }
                catch { return false; }
            }
        }
        public override string ToString()
        {
            return m_Value.ToString();
        }
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Value.GetHashCode();
            hash = hash * 23 + Minimum.GetHashCode();
            hash = hash * 23 + Maximum.GetHashCode();
            return hash;
        }
        #endregion
    }
    [Serializable]
    public class ClampedInt
    {
        #region Public Accessors
        public int Value
        {
            get { return m_Value; }
            set { m_Value = value; clampValue(); }
        }
        public int Minimum
        {
            get { return m_Minimum; }
            set { m_Minimum = Mathf.Min(value, m_Maximum); clampValue(); }
        }
        public int Maximum
        {
            get { return m_Maximum; }
            set { m_Maximum = Mathf.Max(value, m_Minimum); clampValue(); }
        }
        #endregion

        #region Constructors
        public ClampedInt(int in_fValue)
        {
            Value = in_fValue;
            Minimum = 0;
            Maximum = in_fValue;
        }

        public ClampedInt(int in_fValue, int in_fMinimum, int in_fMaximum)
        {
            Value = in_fValue;
            Minimum = in_fMinimum;
            Maximum = in_fMaximum;
        }
        #endregion

        #region Private
        private void clampValue()
        {
            if (m_Value < m_Minimum)
                m_Value = m_Minimum;
            if (m_Value > m_Maximum)
                m_Value = m_Maximum;
        }

        [SerializeField]
        private int m_Value;
        [SerializeField]
        private int m_Minimum;
        [SerializeField]
        private int m_Maximum;
        #endregion

        #region Operator Overloading
        public static int operator +(ClampedInt f1, ClampedInt f2) { return f1.Value + f2.Value; }
        public static int operator -(ClampedInt f1, ClampedInt f2) { return f1.Value - f2.Value; }
        public static int operator *(ClampedInt f1, ClampedInt f2) { return f1.Value * f2.Value; }
        public static int operator /(ClampedInt f1, ClampedInt f2) { return f1.Value / f2.Value; }
        public static int operator +(ClampedInt f1, int f2) { return f1.Value + f2; }
        public static int operator -(ClampedInt f1, int f2) { return f1.Value - f2; }
        public static int operator *(ClampedInt f1, int f2) { return f1.Value * f2; }
        public static int operator /(ClampedInt f1, int f2) { return f1.Value / f2; }
        public static implicit operator ClampedInt(int f) { return new ClampedInt(f); }
        public static implicit operator int(ClampedInt f) { return f.Value; }
        public static bool operator ==(ClampedInt f1, ClampedInt f2) { return f1.Value == f2.Value; }
        public static bool operator !=(ClampedInt f1, ClampedInt f2) { return f1.Value != f2.Value; }
        public static bool operator ==(ClampedInt f1, int f2) { return f1.Value == f2; }
        public static bool operator !=(ClampedInt f1, int f2) { return f1.Value != f2; }
        public static implicit operator string(ClampedInt f) { return f.Value.ToString(); }
        public override bool Equals(object obj)
        {
            try
            {
                return (bool)(this == (ClampedInt)obj);
            }
            catch
            {
                try
                {
                    return (bool)(this == (int)obj);
                }
                catch { return false; }
            }
        }
        public override string ToString()
        {
            return m_Value.ToString();
        }
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Value.GetHashCode();
            hash = hash * 23 + Minimum.GetHashCode();
            hash = hash * 23 + Maximum.GetHashCode();
            return hash;
        }
        #endregion
    }
}