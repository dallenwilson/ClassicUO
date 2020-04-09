﻿#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.Controls
{
    internal class ItemGumpPaperdoll : ItemGump
    {
        private const int MALE_OFFSET = 50000;
        private const int FEMALE_OFFSET = 60000;
        private bool _isPartialHue;

        public ItemGumpPaperdoll(int x, int y, Item item, Mobile owner, bool transparent = false) : base(item)
        {
            X = x;
            Y = y;
            Mobile = owner;
            HighlightOnMouseOver = false;

            Update(item, transparent);
        }

        public Mobile Mobile { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Mobile == null || Mobile.IsDestroyed)
                Dispose();

            if (IsDisposed)
                return;

            Texture.Ticks = (long) totalMS;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Item == null || Item.IsDestroyed)
            {
                Dispose();
            }

            if (IsDisposed)
                return false;

            ResetHueVector();

            ShaderHuesTraslator.GetHueVector(ref _hueVector, Item.Hue & 0x3FFF, _isPartialHue, Alpha, true);

            return batcher.Draw2D(Texture, x, y, ref _hueVector);
        }


        public override bool Contains(int x, int y)
        {
            return Texture?.Contains(x, y) ?? false;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            return base.OnMouseDoubleClick(x, y, button);
        }

        public void Update(Item item, bool transparent = false)
        {
            Alpha = transparent ? 0.5f : 0;

            if (item == null)
            {
                Dispose();
            }

            if (IsDisposed)
                return;

            Item.Graphic = item.Graphic;
            Item.Hue = item.Hue;
            Item.CheckGraphicChange();

            _isPartialHue = item.ItemData.IsPartialHue;

            int offset = !Mobile.IsMale ? FEMALE_OFFSET : MALE_OFFSET;

            ushort id = Item.ItemData.AnimID;
            ushort mobGraphic = Mobile.Graphic;

            if (Client.Version >= ClientVersion.CV_7000 &&
                id == 0x03CA       // graphic for dead shroud
                && Mobile != null && (Mobile.Graphic == 0x02B7 || Mobile.Graphic == 0x02B6)) // dead gargoyle graphics
            {
                id = 0x0223;
            }

            AnimationsLoader.Instance.ConvertBodyIfNeeded(ref mobGraphic);

            if (AnimationsLoader.Instance.EquipConversions.TryGetValue(mobGraphic, out var dict))
            {
                if (dict.TryGetValue(id, out EquipConvData data))
                {
                    if (data.Gump > MALE_OFFSET)
                        id = (ushort)(data.Gump >= FEMALE_OFFSET ? data.Gump - FEMALE_OFFSET : data.Gump - MALE_OFFSET);
                    else
                        id = data.Gump;
                }
            }

            Texture = GumpsLoader.Instance.GetTexture((ushort)(id + offset));

            if (!Mobile.IsMale && Texture == null)
                Texture = GumpsLoader.Instance.GetTexture((ushort)(id + MALE_OFFSET));

            if (Texture == null)
            {
                if (item.Layer != Layer.Face)
                    Log.Error( $"No texture found for Item ({item.Serial.ToHex()}) {item.Graphic.ToHex()} {item.ItemData.Name} {item.Layer}");
                Dispose();

                return;
            }

            Width = Texture.Width;
            Height = Texture.Height;

            WantUpdateSize = false;
        }
    }
}