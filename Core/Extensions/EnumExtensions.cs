﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Vanara.Extensions;

/// <summary>Extensions for enumerated types.</summary>
public static class EnumExtensions
{
	/// <summary>Gets the bit position of a flag that has a single bit, starting at 0.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="flags">The enumerated value.</param>
	/// <returns>The bit position, starting at 0, of the single bit flag specified in <paramref name="flags"/>.</returns>
	/// <exception cref="ArgumentException">The flag value is zero and has no bit position.</exception>
	/// <exception cref="ArithmeticException">The flag value has more than a single bit set.</exception>
	public static byte BitPosition<T>(this T flags) where T : struct, Enum
	{
		CheckHasFlags<T>();
		var flagValue = Convert.ToInt64(flags);
		if (flagValue == 0) throw new ArgumentException("The flag value is zero and has no bit position.");
		var r = Math.Log(flagValue, 2);
		if (r % 1 > 0) throw new ArithmeticException("The flag value has more than a single bit set.");
		return Convert.ToByte(r);
	}

	/// <summary>Throws an exception if a flag value does not exist in a specified enumeration.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="value">The value to check.</param>
	/// <param name="argName">Name of the argument to display in the exception. "value" is used if no value or <c>null</c> is supplied.</param>
	/// <exception cref="InvalidEnumArgumentException"></exception>
	public static void CheckHasValue<T>(T value, string? argName = null) where T : struct, Enum
	{
		if (!IsValid(value))
			throw new InvalidEnumArgumentException(argName ?? "value", Convert.ToInt32(value), typeof(T));
	}

	/// <summary>Clears the specified flags from an enumerated value and returns the new value.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="flags">The enumerated value.</param>
	/// <param name="flag">The flags to clear or unset.</param>
	/// <returns>The resulting enumerated value after the <paramref name="flag"/> has been unset.</returns>
	public static T ClearFlags<T>(this T flags, T flag) where T : struct, Enum => flags.SetFlags(flag, false);

	/// <summary>Combines enumerated list of values into a single enumerated value.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="flags">The flags to combine.</param>
	/// <returns>A single enumerated value.</returns>
	public static T CombineFlags<T>(this IEnumerable<T> flags) where T : struct, Enum
	{
		CheckHasFlags<T>();
		long lValue = 0;
		foreach (var flag in flags)
		{
			var lFlag = Convert.ToInt64(flag);
			lValue |= lFlag;
		}
		return (T)Enum.ToObject(typeof(T), lValue);
	}

	/// <summary>Gets the description supplied by a <see cref="DescriptionAttribute"/> if one is set.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="value">The enumerated value.</param>
	/// <returns>The description, or <c>null</c> if one is not set.</returns>
	public static string? GetDescription<T>(this T value) where T : struct, Enum
	{
		// If this is flag, return flags if there are more than one.
		if (IsFlags<T>() && value.GetFlags().Count() > 1)
			return value.ToString();
		// Get the name or description of the single enum value.
		var name = Enum.GetName(typeof(T), value);
		if (name != null)
		{
			var field = typeof(T).GetField(name);
			if (field != null && Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
				return attr.Description;
		}
		return name;
	}

	/// <summary>Gets the flags of an enumerated value as an enumerated list.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="value">The enumerated value.</param>
	/// <returns>An enumeration of individual flags that compose the <paramref name="value"/>.</returns>
	public static IEnumerable<T> GetFlags<T>(this T value) where T : struct, Enum
	{
		CheckHasFlags<T>();
		foreach (T flag in Enum.GetValues(typeof(T)).OfType<T>())
		{
			if (value.IsFlagSet(flag))
				yield return flag;
		}
	}

	/// <summary>Determines whether the enumerated flag value has the specified flag set.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="flags">The enumerated flag value.</param>
	/// <param name="flag">The flag value to check.</param>
	/// <returns><c>true</c> if is flag set; otherwise, <c>false</c>.</returns>
	public static bool IsFlagSet<T>(this T flags, T flag) where T : struct, Enum
	{
		CheckHasFlags<T>();
		var flagValue = Convert.ToInt64(flag);
		return (Convert.ToInt64(flags) & flagValue) == flagValue;
	}

	/// <summary>Returns an indication if the enumerated value is either defined or can be defined by a set of known flags.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="value">The enumerated value.</param>
	/// <returns><c>true</c> if the specified value is valid; otherwise, <c>false</c>.</returns>
	public static bool IsValid<T>(this T value) where T : struct, Enum
	{
		if (IsFlags<T>())
		{
			long mask = 0, lValue = Convert.ToInt64(value);
			foreach (T flag in Enum.GetValues(typeof(T)).OfType<T>())
				mask |= Convert.ToInt64(flag);
			return (mask & lValue) == lValue;
		}
		return Enum.IsDefined(typeof(T), value);
	}

	/// <summary>Set or unsets flags in a referenced enumerated value.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="flags">A reference to an enumerated value.</param>
	/// <param name="flag">The flag to set or unset.</param>
	/// <param name="set">if set to <c>true</c> sets the flag; otherwise the flag is unset.</param>
	public static void SetFlags<T>(ref T flags, T flag, bool set = true) where T : struct, Enum
	{
		CheckHasFlags<T>();
		var flagsValue = Convert.ToInt64(flags);
		var flagValue = Convert.ToInt64(flag);
		if (set)
			flagsValue |= flagValue;
		else
			flagsValue &= (~flagValue);
		flags = (T)Enum.ToObject(typeof(T), flagsValue);
	}

	/// <summary>Set or unsets flags in an enumerated value and returns the new value.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="flags">The enumerated value.</param>
	/// <param name="flag">The flag to set or unset.</param>
	/// <param name="set">if set to <c>true</c> sets the flag; otherwise the flag is unset.</param>
	/// <returns>The resulting enumerated value after the <paramref name="flag"/> has been set or unset.</returns>
	public static T SetFlags<T>(this T flags, T flag, bool set = true) where T : struct, Enum
	{
		var ret = flags;
		SetFlags(ref ret, flag, set);
		return ret;
	}

	/// <summary>Converts an enumerated value to another fixed type.</summary>
	/// <typeparam name="TEnum">The enumerated type.</typeparam>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>The converted value.</returns>
	/// <exception cref="ArgumentException">The size of TResult cannot be smaller than the size of TEnum.</exception>
	public static TResult To<TEnum, TResult>(this TEnum value) where TEnum : unmanaged, Enum where TResult : unmanaged, IConvertible
	{
		unsafe
		{
			if (sizeof(TResult) < sizeof(TEnum)) throw new ArgumentException("The size of TResult cannot be smaller than the size of TEnum.");
			return *(TResult*)(void*)&value;
		}
	}

	/// <summary>Converts a fixed type to an enumerated value.</summary>
	/// <typeparam name="TValue">The type of the value to convert.</typeparam>
	/// <typeparam name="TEnum">The enumerated type.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>The converted value.</returns>
	/// <exception cref="ArgumentException">The size of TEnum cannot be smaller than the size of TValue.</exception>
	public static TEnum ToEnum<TValue, TEnum>(this TValue value) where TEnum : unmanaged, Enum where TValue : unmanaged, IConvertible
	{
		unsafe
		{
			if (sizeof(TEnum) < sizeof(TValue)) throw new ArgumentException("The size of TEnum cannot be smaller than the size of TValue.");
			return *(TEnum*)(void*)&value;
		}
	}

	/// <summary>Converts a fixed type to an enumerated value.</summary>
	/// <typeparam name="TEnum">The enumerated type.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>The converted value.</returns>
	public static TEnum ToEnum<TEnum>(this byte value) where TEnum : unmanaged, Enum => ToEnum<byte, TEnum>(value);

	/// <summary>Converts a fixed type to an enumerated value.</summary>
	/// <typeparam name="TEnum">The enumerated type.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>The converted value.</returns>
	public static TEnum ToEnum<TEnum>(this sbyte value) where TEnum : unmanaged, Enum => ToEnum<sbyte, TEnum>(value);

	/// <summary>Converts a fixed type to an enumerated value.</summary>
	/// <typeparam name="TEnum">The enumerated type.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>The converted value.</returns>
	public static TEnum ToEnum<TEnum>(this ushort value) where TEnum : unmanaged, Enum => ToEnum<ushort, TEnum>(value);

	/// <summary>Converts a fixed type to an enumerated value.</summary>
	/// <typeparam name="TEnum">The enumerated type.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>The converted value.</returns>
	public static TEnum ToEnum<TEnum>(this short value) where TEnum : unmanaged, Enum => ToEnum<short, TEnum>(value);

	/// <summary>Converts a fixed type to an enumerated value.</summary>
	/// <typeparam name="TEnum">The enumerated type.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>The converted value.</returns>
	public static TEnum ToEnum<TEnum>(this uint value) where TEnum : unmanaged, Enum => ToEnum<uint, TEnum>(value);

	/// <summary>Converts a fixed type to an enumerated value.</summary>
	/// <typeparam name="TEnum">The enumerated type.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>The converted value.</returns>
	public static TEnum ToEnum<TEnum>(this int value) where TEnum : unmanaged, Enum => ToEnum<int, TEnum>(value);

	/// <summary>Converts a byte array to an enumerated value.</summary>
	/// <typeparam name="TEnum">The enumerated type.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>The converted value.</returns>
	public static TEnum ToEnum<TEnum>(this byte[] value) where TEnum : unmanaged, Enum
	{
		int size = Marshal.SizeOf(Enum.GetUnderlyingType(typeof(TEnum)));
		return size switch
		{
			1 => ToEnum<byte, TEnum>(value[0]),
			2 => ToEnum<short, TEnum>(BitConverter.ToInt16(value, 0)),
			4 => ToEnum<int, TEnum>(BitConverter.ToInt32(value, 0)),
			8 => ToEnum<long, TEnum>(BitConverter.ToInt64(value, 0)),
			_ => throw new ArgumentException("Unable to convert bytes to enumerated type.", nameof(value)),
		};
	}

	/// <summary>Checks if <typeparamref name="T"/> represents an enumeration and throws an exception if not.</summary>
	/// <typeparam name="T">The <see cref="Type"/> to validate.</typeparam>
	/// <exception cref="ArgumentException"></exception>
	private static void CheckHasFlags<T>() where T : struct, Enum
	{
		if (!IsFlags<T>())
			throw new ArgumentException($"Type '{typeof(T).FullName}' doesn't have the 'Flags' attribute");
	}

	/// <summary>Determines whether this enumerations has the <see cref="FlagsAttribute"/> set.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <returns><c>true</c> if this instance has the <see cref="FlagsAttribute"/> set; otherwise, <c>false</c>.</returns>
	private static bool IsFlags<T>() where T : struct, Enum => Attribute.IsDefined(typeof(T), typeof(FlagsAttribute));

	/*/// <summary>Returns an indication if the enumerated value is either defined or can be defined by a set of known flags.</summary>
	/// <typeparam name="T">The enumerated type.</typeparam>
	/// <param name="value">The enumerated value.</param>
	/// <param name="validValues">The valid values.</param>
	/// <returns><c>true</c> if the specified value is valid; otherwise, <c>false</c>.</returns>
	private static bool IsValid<T>(this T value, params T[] validValues) where T : struct, IConvertible
	{
		CheckIsEnum<T>();
		foreach (var vval in validValues)
			if (value.Equals(vval))
				return true;
		return false;
	}*/
}