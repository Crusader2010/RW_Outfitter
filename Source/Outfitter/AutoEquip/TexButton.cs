﻿using System;
using UnityEngine;

namespace Verse
{
    [StaticConstructorOnStartup]
    internal class TexButton
    {
         public static readonly Texture2D Info = ContentFinder<Texture2D>.Get("UI/Buttons/InfoButton", true);

        public static readonly Texture2D Drop = ContentFinder<Texture2D>.Get("UI/Buttons/Drop", true);

        public static readonly Texture2D FloatRangeSliderTex = ContentFinder<Texture2D>.Get("UI/Widgets/RangeSlider", true);

        public static Texture2D resetButton = ContentFinder<Texture2D>.Get("reset");

        public static Texture2D deleteButton = ContentFinder<Texture2D>.Get("delete");

        public static Texture2D addButton = ContentFinder<Texture2D>.Get("add");
    }
}
