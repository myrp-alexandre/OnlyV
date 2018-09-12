﻿namespace OnlyV.Themes.Common.Specs
{
    // ReSharper disable MemberCanBePrivate.Global
    public class OnlyVTitleTextSpec
    {
        public OnlyVTitleTextSpec()
        {
            Font = new OnlyVFontSpec
            {
                Colour = "#fbfbff",
                Size = 64
            };

            HorizontalAlignment = OnlyVHorizontalTextAlignment.Right;
            Position = OnlyVTitlePosition.Bottom;
            DropShadow = new OnlyVDropShadowSpec { Show = false };
        }

        public OnlyVFontSpec Font { get; set; }

        public OnlyVHorizontalTextAlignment HorizontalAlignment { get; set; }

        public OnlyVTitlePosition Position { get; set; }

        public OnlyVDropShadowSpec DropShadow { get; set; }
    }
}
