/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


/*
* Flag images from: https://github.com/MikeCodesDotNET/BlazorFlags
* License: MIT
* No sourcecode taken over; pixel offset reformatted.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

// ReSharper disable UnusedType.Global

namespace AasxIntegrationBaseWpf
{
    public static class CountryFlagWpf
    {
        public static string ResourcePathFlags = "";

        public static BitmapImage FlagsImage = null;

        public class FlagInfo
        {
            /// <summary>
            /// Code according ISO 3166-2
            /// </summary>
            public string Code2 = ""; 

            /// <summary>
            /// Pixel offset 
            /// </summary>
            public int Offset = 0;
        }

        public static Dictionary<string, FlagInfo> Code2ToFlagInfo = new Dictionary<string, FlagInfo>();

        static CountryFlagWpf()
        {
            InitFI();
            LoadImage();
        }

        public static void LoadImage()
        {
            try
            {
                FlagsImage = new BitmapImage(
                        new Uri("pack://application:,,,/AasxIntegrationBaseWpf;component/Resources/country_flags.png", UriKind.RelativeOrAbsolute));

                if (FlagsImage.PixelWidth < 1)
                    FlagsImage = null;
            }
            catch (Exception ex)
            {
                FlagsImage = null;
                LogInternally.That.CompletelyIgnoredError(ex);
            }
        }

        public static void AddFI(string code3, string code2, int ofs)
        {
            code2 = code2?.ToLower().Trim();
            Code2ToFlagInfo.Add(code2, new FlagInfo() { Code2 = code2, Offset = ofs });
        }

        public static void InitFI()
        {
            Code2ToFlagInfo.Clear();
            AddFI("", "ad", ofs: 42);
            AddFI("are", "ae", ofs: 82);
            AddFI("afg", "af", ofs: 123);
            AddFI("atg", "ag", ofs: 164);
            AddFI("aia", "ai", ofs: 205);
            AddFI("alb", "al", ofs: 246);
            AddFI("arm", "am", ofs: 287);
            AddFI("ant", "an", ofs: 328);
            AddFI("ago", "ao", ofs: 369);
            AddFI("ata", "aq", ofs: 410);
            AddFI("arg", "ar", ofs: 451);
            AddFI("asm", "as", ofs: 492);
            AddFI("aut", "at", ofs: 533);
            AddFI("aus", "au", ofs: 574);
            AddFI("abw", "aw", ofs: 615);
            AddFI("ala", "ax", ofs: 657);
            AddFI("aze", "az", ofs: 698);
            AddFI("bih", "ba", ofs: 738);
            AddFI("brb", "bb", ofs: 779);
            AddFI("bgd", "bd", ofs: 820);
            AddFI("bel", "be", ofs: 861);
            AddFI("bfa", "bf", ofs: 902);
            AddFI("bgr", "bg", ofs: 943);
            AddFI("bhr", "bh", ofs: 984);
            AddFI("bdi", "bi", ofs: 1025);
            AddFI("ben", "bj", ofs: 1067);
            AddFI("blm", "bl", ofs: 1107);
            AddFI("bmu", "bm", ofs: 1148);
            AddFI("brn", "bn", ofs: 1189);
            AddFI("bol", "bo", ofs: 1230);
            AddFI("bes", "bq", ofs: 1272);
            AddFI("bra", "br", ofs: 1312);
            AddFI("bhs", "bs", ofs: 1353);
            AddFI("btn", "bt", ofs: 1394);
            AddFI("bvt", "bv", ofs: 1435);
            AddFI("bwa", "bw", ofs: 1477);
            AddFI("blr", "by", ofs: 1517);
            AddFI("blz", "bz", ofs: 1558);
            AddFI("can", "ca", ofs: 1599);
            AddFI("cck", "cc", ofs: 1640);
            AddFI("cod", "cd", ofs: 1681);
            AddFI("caf", "cf", ofs: 1722);
            AddFI("cog", "cg", ofs: 1763);
            AddFI("che", "ch", ofs: 1804);
            AddFI("civ", "ci", ofs: 1845);
            AddFI("cok", "ck", ofs: 1886);
            AddFI("chl", "cl", ofs: 1927);
            AddFI("cmr", "cm", ofs: 1968);
            AddFI("chn", "cn", ofs: 2009);
            AddFI("col", "co", ofs: 2050);
            AddFI("cri", "cr", ofs: 2091);
            AddFI("cub", "cu", ofs: 2132);
            AddFI("cpv", "cv", ofs: 2173);
            AddFI("cuw", "cw", ofs: 2214);
            AddFI("cxr", "cx", ofs: 2255);
            AddFI("cyp", "cy", ofs: 2296);
            AddFI("cze", "cz", ofs: 2337);
            AddFI("deu", "de", ofs: 2377);
            AddFI("dji", "dj", ofs: 2419);
            AddFI("dnk", "dk", ofs: 2460);
            AddFI("dma", "dm", ofs: 2501);
            AddFI("dom", "do", ofs: 2542);
            AddFI("dza", "dz", ofs: 2583);
            AddFI("ecu", "ec", ofs: 2624);
            AddFI("est", "ee", ofs: 2665);
            AddFI("egy", "eg", ofs: 2706);
            AddFI("esh", "eh", ofs: 2747);
            AddFI("eri", "er", ofs: 2787);
            AddFI("", "es-ca", ofs: 2829);
            AddFI("esp", "es", ofs: 2870);
            AddFI("eth", "et", ofs: 2911);
            AddFI("", "eu", ofs: 2953);
            AddFI("fin", "fi", ofs: 2993);
            AddFI("fji", "fj", ofs: 3034);
            AddFI("flk", "fk", ofs: 3075);
            AddFI("fsm", "fm", ofs: 3116);
            AddFI("fro", "fo", ofs: 3157);
            AddFI("fra", "fr", ofs: 3198);
            AddFI("gab", "ga", ofs: 3239);
            AddFI("", "gb-eng", ofs: 3280);
            AddFI("", "gb-nir", ofs: 3321);
            AddFI("", "gb-sct", ofs: 3362);
            AddFI("", "gb-wls", ofs: 3403);
            AddFI("gbr", "gb", ofs: 3444);
            AddFI("grd", "gd", ofs: 3485);
            AddFI("geo", "ge", ofs: 3526);
            AddFI("guf", "gf", ofs: 3567);
            AddFI("ggy", "gg", ofs: 3608);
            AddFI("gha", "gh", ofs: 3649);
            AddFI("gib", "gi", ofs: 3690);
            AddFI("grl", "gl", ofs: 3731);
            AddFI("gmb", "gm", ofs: 3771);
            AddFI("gin", "gn", ofs: 3813);
            AddFI("glp", "gp", ofs: 3854);
            AddFI("gnq", "gq", ofs: 3895);
            AddFI("grc", "gr", ofs: 3936);
            AddFI("sgs", "gs", ofs: 3977);
            AddFI("gtm", "gt", ofs: 4018);
            AddFI("gum", "gu", ofs: 4059);
            AddFI("gnb", "gw", ofs: 4100);
            AddFI("guy", "gy", ofs: 4141);
            AddFI("hkg", "hk", ofs: 4182);
            AddFI("hmd", "hm", ofs: 4223);
            AddFI("hnd", "hn", ofs: 4264);
            AddFI("hrv", "hr", ofs: 4305);
            AddFI("hti", "ht", ofs: 4347);
            AddFI("hun", "hu", ofs: 4387);
            AddFI("idn", "id", ofs: 4428);
            AddFI("irl", "ie", ofs: 4468);
            AddFI("isr", "il", ofs: 4510);
            AddFI("imn", "im", ofs: 4551);
            AddFI("ind", "in", ofs: 4593);
            AddFI("iot", "io", ofs: 4633);
            AddFI("irq", "iq", ofs: 4674);
            AddFI("irn", "ir", ofs: 4715);
            AddFI("isl", "is", ofs: 4756);
            AddFI("ita", "it", ofs: 4797);
            AddFI("jey", "je", ofs: 4838);
            AddFI("jam", "jm", ofs: 4879);
            AddFI("jor", "jo", ofs: 4920);
            AddFI("jpn", "jp", ofs: 4961);
            AddFI("ken", "ke", ofs: 5002);
            AddFI("kgz", "kg", ofs: 5043);
            AddFI("khm", "kh", ofs: 5084);
            AddFI("kir", "ki", ofs: 5125);
            AddFI("com", "km", ofs: 5166);
            AddFI("kna", "kn", ofs: 5207);
            AddFI("prk", "kp", ofs: 5248);
            AddFI("kor", "kr", ofs: 5289);
            AddFI("kwt", "kw", ofs: 5330);
            AddFI("cym", "ky", ofs: 5371);
            AddFI("kaz", "kz", ofs: 5412);
            AddFI("lao", "la", ofs: 5453);
            AddFI("lbn", "lb", ofs: 5494);
            AddFI("lca", "lc", ofs: 5535);
            AddFI("lie", "li", ofs: 5576);
            AddFI("lka", "lk", ofs: 5617);
            AddFI("lbr", "lr", ofs: 5658);
            AddFI("lso", "ls", ofs: 5698);
            AddFI("ltu", "lt", ofs: 5740);
            AddFI("lux", "lu", ofs: 5781);
            AddFI("lva", "lv", ofs: 5822);
            AddFI("lby", "ly", ofs: 5862);
            AddFI("mar", "ma", ofs: 5904);
            AddFI("mco", "mc", ofs: 5945);
            AddFI("mda", "md", ofs: 5986);
            AddFI("mne", "me", ofs: 6028);
            AddFI("maf", "mf", ofs: 6068);
            AddFI("mdg", "mg", ofs: 6109);
            AddFI("mhl", "mh", ofs: 6150);
            AddFI("mkd", "mk", ofs: 6191);
            AddFI("mli", "ml", ofs: 6233);
            AddFI("mmr", "mm", ofs: 6273);
            AddFI("mng", "mn", ofs: 6314);
            AddFI("mac", "mo", ofs: 6355);
            AddFI("mnp", "mp", ofs: 6397);
            AddFI("mtq", "mq", ofs: 6437);
            AddFI("mrt", "mr", ofs: 6478);
            AddFI("msr", "ms", ofs: 6519);
            AddFI("mlt", "mt", ofs: 6560);
            AddFI("mus", "mu", ofs: 6601);
            AddFI("mdv", "mv", ofs: 6642);
            AddFI("mwi", "mw", ofs: 6682);
            AddFI("mex", "mx", ofs: 6724);
            AddFI("mys", "my", ofs: 6765);
            AddFI("moz", "mz", ofs: 6806);
            AddFI("nam", "na", ofs: 6847);
            AddFI("ncl", "nc", ofs: 6888);
            AddFI("ner", "ne", ofs: 6929);
            AddFI("nfk", "nf", ofs: 6970);
            AddFI("nga", "ng", ofs: 7011);
            AddFI("nic", "ni", ofs: 7052);
            AddFI("nld", "nl", ofs: 7093);
            AddFI("nor", "no", ofs: 7134);
            AddFI("npl", "np", ofs: 7175);
            AddFI("nru", "nr", ofs: 7217);
            AddFI("niu", "nu", ofs: 7257);
            AddFI("nzl", "nz", ofs: 7298);
            AddFI("omn", "om", ofs: 7339);
            AddFI("pan", "pa", ofs: 7380);
            AddFI("per", "pe", ofs: 7421);
            AddFI("pyf", "pf", ofs: 7463);
            AddFI("png", "pg", ofs: 7503);
            AddFI("phl", "ph", ofs: 7544);
            AddFI("pak", "pk", ofs: 7585);
            AddFI("pol", "pl", ofs: 7626);
            AddFI("spm", "pm", ofs: 7667);
            AddFI("pcn", "pn", ofs: 7708);
            AddFI("pri", "pr", ofs: 7749);
            AddFI("pse", "ps", ofs: 7790);
            AddFI("prt", "pt", ofs: 7831);
            AddFI("plw", "pw", ofs: 7873);
            AddFI("pry", "py", ofs: 7913);
            AddFI("qat", "qa", ofs: 7954);
            AddFI("reu", "re", ofs: 7995);
            AddFI("rou", "ro", ofs: 8036);
            AddFI("srb", "rs", ofs: 8077);
            AddFI("rus", "ru", ofs: 8117);
            AddFI("rwa", "rw", ofs: 8159);
            AddFI("sau", "sa", ofs: 8200);
            AddFI("slb", "sb", ofs: 8241);
            AddFI("syc", "sc", ofs: 8282);
            AddFI("sdn", "sd", ofs: 8323);
            AddFI("swe", "se", ofs: 8364);
            AddFI("sgp", "sg", ofs: 8405);
            AddFI("shn", "sh", ofs: 8446);
            AddFI("svn", "si", ofs: 8487);
            AddFI("sjm", "sj", ofs: 8528);
            AddFI("svk", "sk", ofs: 8569);
            AddFI("sle", "sl", ofs: 8610);
            AddFI("smr", "sm", ofs: 8651);
            AddFI("sen", "sn", ofs: 8693);
            AddFI("som", "so", ofs: 8733);
            AddFI("sur", "sr", ofs: 8774);
            AddFI("ssd", "ss", ofs: 8815);
            AddFI("stp", "st", ofs: 8856);
            AddFI("slv", "sv", ofs: 8897);
            AddFI("sxm", "sx", ofs: 8938);
            AddFI("syr", "sy", ofs: 8979);
            AddFI("swz", "sz", ofs: 9020);
            AddFI("tca", "tc", ofs: 9061);
            AddFI("tcd", "td", ofs: 9102);
            AddFI("atf", "tf", ofs: 9142);
            AddFI("tgo", "tg", ofs: 9184);
            AddFI("tha", "th", ofs: 9225);
            AddFI("tjk", "tj", ofs: 9266);
            AddFI("tkl", "tk", ofs: 9307);
            AddFI("tls", "tl", ofs: 9348);
            AddFI("tkm", "tm", ofs: 9389);
            AddFI("tun", "tn", ofs: 9430);
            AddFI("ton", "to", ofs: 9472);
            AddFI("tur", "tr", ofs: 9512);
            AddFI("tto", "tt", ofs: 9552);
            AddFI("tuv", "tv", ofs: 9594);
            AddFI("twn", "tw", ofs: 9635);
            AddFI("tza", "tz", ofs: 9676);
            AddFI("ukr", "ua", ofs: 9717);
            AddFI("uga", "ug", ofs: 9757);
            AddFI("umi", "um", ofs: 9799);
            AddFI("", "un", ofs: 9840);
            AddFI("usa", "us", ofs: 9881);
            AddFI("ury", "uy", ofs: 9922);
            AddFI("uzb", "uz", ofs: 9963);
            AddFI("vat", "va", ofs: 10004);
            AddFI("vct", "vc", ofs: 10045);
            AddFI("ven", "ve", ofs: 10086);
            AddFI("vgb", "vg", ofs: 10127);
            AddFI("vir", "vi", ofs: 10168);
            AddFI("vnm", "vn", ofs: 10209);
            AddFI("vut", "vu", ofs: 10250);
            AddFI("wlf", "wf", ofs: 10291);
            AddFI("wsm", "ws", ofs: 10331);
            AddFI("", "xk", ofs: 10373);
            AddFI("yem", "ye", ofs: 10414);
            AddFI("myt", "yt", ofs: 10455);
            AddFI("zaf", "za", ofs: 10496);
            AddFI("zmb", "zm", ofs: 10538);
            AddFI("zwe", "zw", ofs: 10578);
            AddFI("sun", "su", ofs: 10619);
        }

        public static CroppedBitmap GetCroppedFlag(string code2)
        {
            code2 = code2?.ToLower().Trim();
            if (FlagsImage == null || code2?.HasContent() != true || 
                !Code2ToFlagInfo.ContainsKey(code2))
                return null;
            var fi = Code2ToFlagInfo[code2];

            return new CroppedBitmap(FlagsImage, new Int32Rect(0, fi.Offset, FlagsImage.PixelWidth, 40));
        }
    }
}
