﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Assignable;

namespace Virial.Configuration;

public interface Configuration
{

}

public enum FilterAction
{
    And,
    Or,
    Set
}

public interface ModifierFilter : Configuration
{
    void Filter(FilterAction filterAction, params DefinedModifier[] modifiers);
    bool Test(DefinedModifier modifier);
}

public interface RoleFilter : Configuration
{
    void Filter(FilterAction filterAction, params DefinedRole[] roles);
    bool Test(DefinedRole role);
}

/// <summary>
/// 値を持つオプションです。
/// </summary>
public interface ValueConfiguration
{
    /// <summary>
    /// 現在の値を文字列で取得します。
    /// </summary>
    string CurrentValue { get; }

    /// <summary>
    /// 現在の値を実数型で取得します。
    /// 実数型のオプションでない場合、取得に失敗し、例外が発生します。
    /// </summary>
    /// <returns>設定値</returns>
    float AsFloat();

    /// <summary>
    /// 現在の値を整数型で取得します。
    /// 整数型のオプションでない場合、取得に失敗し、例外が発生します。
    /// </summary>
    /// <returns>設定値</returns>
    int AsInt();

    /// <summary>
    /// 値を整数で指定します。ホストのみ使用可能です。
    /// </summary>
    /// <param name="value">新たな設定値</param>
    /// <returns>成功した場合true</returns>
    bool UpdateValue(int value);

    /// <summary>
    /// 値を実数で指定します。ホストのみ使用可能です。
    /// </summary>
    /// <param name="value">新たな設定値</param>
    /// <returns>成功した場合true</returns>
    bool UpdateValue(float value);

    /// <summary>
    /// 値を真偽値で指定します。ホストのみ使用可能です。
    /// </summary>
    /// <param name="value">新たな設定値</param>
    /// <returns>成功した場合true</returns>
    bool UpdateValue(bool value);

    /// <summary>
    /// 値を文字列で指定します。ホストのみ使用可能です。
    /// </summary>
    /// <param name="value">新たな設定値</param>
    /// <returns>成功した場合true</returns>
    bool UpdateValue(string value);

}