using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GET_PAL
{
    public partial class Form1 : Form
    {

        public static int[] Out_Array = new int[65536]; // for saving files
        public static int out_size;

        public const int FLOYD_STEIN = 0;
        public const int BAYER8 = 1;
        public readonly int[,] BAYER_MATRIX =
        {
            { 0,48,12,60,3,51,15,63 },
            { 32,16,44,28,35,19,47,31 },
            { 8,56,4,52,11,59,7,55 },
            { 40,24,36,20,43,27,39,23 },
            { 2,50,14,62,1,49,13,61 },
            { 34,18,46,30,33,17,45,29 },
            { 10,58,6,54,9,57,5,53 },
            { 42,26,38,22,41,25,37,21 }
        }; // 1/64 times this

        //public const int BAYER_MULT = 64;
        public static int dither_factor = 0;
        public static int dither_adjust = 0;
        public static double dither_db = 0.0;

        // RGB palette, with black put at 0 and grays moved down
        // 13 x 4 = 52 colors x 3 = 156
        // palette from FBX, FirebrandX Smooth NES palette
        public static int[] NES_PALETTE = new int[156] {
            0,0,0,
            0x00, 0x13, 0x80,
            0x1e, 0x00, 0x8a,
            0x39, 0x00, 0x7a,
            0x55, 0x00, 0x56,
            0x5a, 0x00, 0x18,
            0x4f, 0x10, 0x00,
            0x3d, 0x1c, 0x00,
            0x25, 0x32, 0x00,
            0x00, 0x3d, 0x00,
            0x00, 0x40, 0x00,
            0x00, 0x39, 0x24,
            0x00, 0x2e, 0x55,

            0x6a, 0x6d, 0x6a,
            0x18, 0x50, 0xc7,
            0x4b, 0x30, 0xe3,
            0x73, 0x22, 0xd6,
            0x95, 0x1f, 0xa9,
            0x9d, 0x28, 0x5c,
            0x98, 0x37, 0x00,
            0x7f, 0x4c, 0x00,
            0x5e, 0x64, 0x00,
            0x22, 0x77, 0x00,
            0x02, 0x7e, 0x02,
            0x00, 0x76, 0x45,
            0x00, 0x6e, 0x8a,

            0xb9, 0xbc, 0xb9,
            0x68, 0xa6, 0xff,
            0x8c, 0x9c, 0xff,
            0xb5, 0x86, 0xff,
            0xd9, 0x75, 0xfd,
            0xe3, 0x77, 0xb9,
            0xe5, 0x8d, 0x68,
            0xd4, 0x9d, 0x29,
            0xb3, 0xaf, 0x0c,
            0x7b, 0xc2, 0x11,
            0x55, 0xca, 0x47,
            0x46, 0xcb, 0x81,
            0x47, 0xc1, 0xc5,

            0xff, 0xff, 0xff,
            0xcc, 0xea, 0xff,
            0xdd, 0xde, 0xff,
            0xec, 0xda, 0xff,
            0xf8, 0xd7, 0xfe,
            0xfc, 0xd6, 0xf5,
            0xfd, 0xdb, 0xcf,
            0xf9, 0xe7, 0xb5,
            0xf1, 0xf0, 0xaa,
            0xda, 0xfa, 0xa9,
            0xc9, 0xff, 0xbc,
            0xc3, 0xfb, 0xd7,
            0xc4, 0xf6, 0xf6
        };
        // note, unlike the real NES colors
        // 0 = black
        // d = dark gray
        // 1a = light gray
        // 27 = white

        // conversion chart, all numbers in hex
        // 0-c = 0-c
        // d = $10 --
        // e = $11
        // f = $12
        // 10 = $13
        // 11 = $14
        // 12 = $15
        // 13 = $16
        // 14 = $17
        // 15 = $18
        // 16 = $19
        // 17 = $1a
        // 18 = $1b
        // 19 = $1c
        // 1a = $20 --
        // 1b = $21
        // 1c = $22
        // 1d = $23
        // 1e = $24
        // 1f = $25
        // 20 = $26
        // 21 = $27
        // 22 = $28
        // 23 = $29
        // 24 = $2a
        // 25 = $2b
        // 26 = $2c
        // 27 = $30 -- white
        // 28 = $31
        // 29 = $32
        // 2a = $33
        // 2b = $34
        // 2c = $35
        // 2d = $36
        // 2e = $37
        // 2f = $38
        // 30 = $39
        // 31 = $3a
        // 32 = $3b
        // 33 = $3c
        // later, white is changed to 99 (0x63), so it sorts to the top

        public static Color sel_color = Color.Black;
        public static Color over_color = Color.Black; // override

        public static Color[] col_array = new Color[13] {
        Color.Black, Color.Black, Color.Black, Color.Black,
        Color.Black, Color.Black, Color.Black, Color.Black,
        Color.Black, Color.Black, Color.Black, Color.Black,
        Color.Black
        };
        public static Color[] col_array4 = new Color[4] {
        Color.Black, Color.Black, Color.Black, Color.Black
        };
        public static Color[] col_array_extra = new Color[6] {
        Color.Black, Color.Black, Color.Black, Color.Black,
        Color.Black, Color.Black
        };
        public static int[] nes_array = new int[13] // reduce to 13 color
        {
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0
        };
        //public static int[] nes_array2 = new int[13] // just a copy of nes array
        //{
        //    0,0,0,0, 0,0,0,0, 0,0,0,0, 0
        //};
        public static int[] final_array = new int[13] // 13 colors after testing
        {
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0
        };
        public static int[] cnt_array = new int[13] // count each color
        {
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0
        };
        public static int[] col_val = new int[19] // final values used, 6 extra ??
        {
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0, 0,0,0,0,0,0
        };
        public static int[] extra_array = new int[6] // extra palette
        {
            0,0,0,0, 0,0
        };
        public static int[] col_val_extra = new int[6] // final values used, 6 extra
        {
            0,0,0,0, 0,0
        };
        //public static int forbid_index1, forbid_index2;

        public static int has_loaded = 0;
        public static int has_converted = 0;

        const int MAX_WIDTH = 256;
        const int MAX_HEIGHT = 240;

        public static Bitmap orig_bmp = new Bitmap(MAX_WIDTH, MAX_HEIGHT);
        public static Bitmap bright_bmp = new Bitmap(MAX_WIDTH, MAX_HEIGHT); // brightness adjust
        public static Bitmap p1_bmp = new Bitmap(MAX_WIDTH, MAX_HEIGHT); // palette 1
        public static Bitmap p2_bmp = new Bitmap(MAX_WIDTH, MAX_HEIGHT); // etc.
        public static Bitmap p3_bmp = new Bitmap(MAX_WIDTH, MAX_HEIGHT);
        public static Bitmap p4_bmp = new Bitmap(MAX_WIDTH, MAX_HEIGHT);
        public static Bitmap all_bmp = new Bitmap(MAX_WIDTH, MAX_HEIGHT); // combined
        public static Bitmap scratch_bmp = new Bitmap(MAX_WIDTH, MAX_HEIGHT); // dither scratchpad
        public static Bitmap left_bmp = new Bitmap(MAX_WIDTH, MAX_HEIGHT); // final visible bmp

        public static Bitmap NES13_bmp = new Bitmap(MAX_WIDTH, MAX_HEIGHT); // closest 13 colors

        public static int[] nametable = new int[32 * 30]; // contiguous and wrong
        public static int[] nametable2 = new int[32 * 30]; // sized correctly to image
        public static int[] attr_table = new int[64]; // final bytes 
        public static int[] attr_table2 = new int[16 * 15]; // uncompressed

        //public static int[] mark_array = new int[16 * 15]; // mark used palettes

        public static int image_width, image_height, image_width8, image_height8;
        public static int remember_index, num_tiles;
        // 65536 = 256x256 or 4 tilesets of 128x128, max possible
        public static int[] p1_CHR = new int[65536]; // for CHR output
        public static int[] p2_CHR = new int[65536]; // for CHR output
        public static int[] p3_CHR = new int[65536]; // for CHR output
        public static int[] p4_CHR = new int[65536]; // for CHR output
        public static int[] all_CHR = new int[65536]; // for CHR output
        public static int[] final_CHR = new int[65536]; // for CHR output
        public static int[] CHR_16bytes = new int[16];

        // for auto color generator
        public static int[] Count_Array = new int[52]; // 65536 count each color
        public static int color_count, color_count2; // how many total different colors
        public static int r_val, g_val, b_val, diff_val, sel_color_val;
        public static int c_offset, c_offset2;
        public static int[] Ar_12x12 = new int[144]; // 12 color x 12 color
        public static int[] Temp_Palettes = new int[72]; // each 16x16 block
        // 72 = 12 colors * 3 colors * 2 sets
        public static int[] Final_Palettes = new int[72]; // each 16x16 block
        public static int[] Count_Palettes = new int[72]; // each 16x16 block
        public static int[] BMP_as_val = new int[61440];
        public static int palette_index = 0;
        //public static int palette_index2 = 0;
        public static int count_palettes = 0;

        public static int chr_index, nt_size, skip_combo;
        public static int bright_adj = 0;
        
        public static int over_val = 0; // override value
        public static bool override_cb = false;
        public static bool pal1_on = true;
        public static bool pal2_on = true;
        public static bool pal3_on = true;
        public static bool pal4_on = true;
        public static int lt_click_mode = 0; // 0 = set, 1 = rotate
        // and rt click should always be a get

        


        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 1; // bayer
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox4.DropDownStyle = ComboBoxStyle.DropDownList;
            label3.Focus();
            this.ActiveControl = label3;
        }


        public int Clamp255(int col)
        {
            if (col > 255) col = 255;
            if (col < 0) col = 0;
            return col;
        }

        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
        { // save left image
            if (has_loaded == 0)
            {
                MessageBox.Show("No image loaded.");
                label3.Focus();
                return;
            }
            if (has_converted == 0)
            {
                MessageBox.Show("Image hasn't converted yet.");
                label3.Focus();
                return;
            }

            // save the left_bmp
            Rectangle cloneRect = new Rectangle(0, 0, image_width, image_height);
            System.Drawing.Imaging.PixelFormat format = left_bmp.PixelFormat;
            Bitmap cloneBMP = left_bmp.Clone(cloneRect, format);


            // open dialogue
            // save file
            // export image pic of the current view
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Filter = "PNG|*.png|BMP|*.bmp|JPG|*.jpg|GIF|*.gif";

            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string ext = System.IO.Path.GetExtension(sfd.FileName);
                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg":
                        cloneBMP.Save(sfd.FileName, ImageFormat.Jpeg);
                        break;
                    case ".bmp":
                        cloneBMP.Save(sfd.FileName, ImageFormat.Bmp);
                        break;
                    case ".gif":
                        cloneBMP.Save(sfd.FileName, ImageFormat.Gif);
                        break;
                    default:
                        cloneBMP.Save(sfd.FileName, ImageFormat.Png);
                        break;

                }
            }
            label3.Focus();
        }

        




        private void saveCHR1RawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((has_loaded == 0) || (has_converted == 0))
            {
                MessageBox.Show("Image hasn't converted yet.");
                label3.Focus();
                return;
            }

            comboBox2.SelectedIndex = 1;
            save_one_chr();

            label3.Focus();
        }

        private void saveCHR2RawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((has_loaded == 0) || (has_converted == 0))
            {
                MessageBox.Show("Image hasn't converted yet.");
                label3.Focus();
                return;
            }

            comboBox2.SelectedIndex = 2;
            save_one_chr();

            label3.Focus();
        }

        private void saveCHR3RawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((has_loaded == 0) || (has_converted == 0))
            {
                MessageBox.Show("Image hasn't converted yet.");
                label3.Focus();
                return;
            }

            comboBox2.SelectedIndex = 3;
            save_one_chr();

            label3.Focus();
        }

        private void saveCHR4RawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((has_loaded == 0) || (has_converted == 0))
            {
                MessageBox.Show("Image hasn't converted yet.");
                label3.Focus();
                return;
            }

            comboBox2.SelectedIndex = 4;
            save_one_chr();

            label3.Focus();
        }

        private void saveFinalCHRToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((has_loaded == 0) || (has_converted == 0))
            {
                MessageBox.Show("Image hasn't converted yet.");
                label3.Focus();
                return;
            }
            if(nt_size < 1) // error, should be at least 1
            {
                MessageBox.Show("Error. Too few tiles??");
                label3.Focus();
                return;
            }

            comboBox2.SelectedIndex = 0;
            out_size = 0;

            // final_CHR[65536] y*256 + x

            // just go by nt_size = # of final tiles

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "chr File (*.chr)|*.chr|DZ4 (*.dz4)|*.dz4";
            saveFileDialog1.Title = "Save the CHR";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile();

                // do each 128x128 block separately
                // always do the top left 128x128
                int x, y;

                c_offset = 0;
                c_offset2 = 0;

                for (int i = 0; i < nt_size; i++)
                {
                    y = (i / 32) * 8;
                    x = (i % 32) * 8;

                    // process each 8x8 tile separately
                    Dry_CHR_Loop2(x, y);

                    // have all 16 bytes, save them to file
                    for (int j = 0; j < 16; j++)
                    {
                        Out_Array[out_size] = CHR_16bytes[j];
                        out_size++;
                        
                    }

                }

                if(checkBox2.Checked == true)
                {
                    // pad to nearest $1000
                    int wrong = (nt_size % 256);
                    if (wrong != 0)
                    {
                        wrong *= 16;
                        wrong = 4096 - wrong;

                        for (int j = 0; j < wrong; j++)
                        {
                            Out_Array[out_size] = 0; // CHR_16bytes[j];
                            out_size++;
                            
                        }
                    }
                }

                // now save out_array, is it compressed ?
                string ext = System.IO.Path.GetExtension(saveFileDialog1.FileName);
                if (ext == ".chr")
                {
                    for (int i = 0; i < out_size; i++)
                    {
                        fs.WriteByte((byte)Out_Array[i]);
                    }
                }
                else // compress dz4
                {
                    if (CompressIt(Out_Array, out_size) == 1)
                    {
                        // now in rle_array, rle_size
                        for (int i = 0; i < rle_size; i++)
                        {
                            fs.WriteByte(rle_array[i]);
                        }
                    }
                    else
                    {
                        //MessageBox.Show("Something went wrong?");
                        //should already be a message
                    }
                }

                fs.Close();
            }

            label3.Focus();
        }

        private void saveNametableToolStripMenuItem1_Click(object sender, EventArgs e)
        { // just the nametable
            if ((has_loaded == 0) || (has_converted == 0))
            {
                MessageBox.Show("Image hasn't converted yet.");
                label3.Focus();
                return;
            }

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Nametable (*.nam)|*.nam|DZ4 (*.dz4)|*.dz4";
            saveFileDialog1.Title = "Save the Nametable";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile();

                // now save array, is it compressed ?
                string ext = System.IO.Path.GetExtension(saveFileDialog1.FileName);
                if (ext == ".nam")
                {
                    for (int i = 0; i < 960; i++)
                    {
                        fs.WriteByte((byte)nametable2[i]);
                    }
                }
                else // compress dz4
                {
                    if (CompressIt(nametable2, 960) == 1)
                    {
                        // now in rle_array, rle_size
                        for (int i = 0; i < rle_size; i++)
                        {
                            fs.WriteByte(rle_array[i]);
                        }
                    }
                    else
                    {
                        //MessageBox.Show("Something went wrong?");
                        //should already be a message
                    }
                }

                fs.Close();
            }

            label3.Focus();
        }

        private void saveAttribTableToolStripMenuItem_Click(object sender, EventArgs e)
        { // just the attribute table
            if ((has_loaded == 0) || (has_converted == 0))
            {
                MessageBox.Show("Image hasn't converted yet.");
                label3.Focus();
                return;
            }
            // process the attribute table to 64 bytes
            crunch_AT();

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Nametable (*.nam)|*.nam|DZ4 (*.dz4)|*.dz4";
            saveFileDialog1.Title = "Save the Attrib Table";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile();

                // now save array, is it compressed ?
                string ext = System.IO.Path.GetExtension(saveFileDialog1.FileName);
                if (ext == ".nam")
                {
                    for (int i = 0; i < 64; i++)
                    {
                        fs.WriteByte((byte)attr_table[i]);
                    }
                }
                else // compress dz4
                {
                    if (CompressIt(attr_table, 64) == 1)
                    {
                        // now in rle_array, rle_size
                        for (int i = 0; i < rle_size; i++)
                        {
                            fs.WriteByte(rle_array[i]);
                        }
                    }
                    else
                    {
                        //MessageBox.Show("Something went wrong?");
                        //should already be a message
                    }
                }

                fs.Close();
            }

            label3.Focus();
        }

        public void crunch_AT()
        {
            // attr_table[64] is a 8x8 matrix
            // attr_table2[240] is a 16x15 matrix
            // convert attr_table2 to attr_table, the actual AT size.
            // blank it, why not
            for (int i = 0; i < 64; i++)
            {
                attr_table[i] = 0;
            }
            // crunch it now
            int at_offset = 0;
            int at_val = 0;
            int bit01, bit23, bit45, bit67;

            for(int y = 0; y < 8; y++)
            {
                for(int x = 0; x < 8; x++)
                {
                    bit01 = attr_table2[(y * 32) + (x * 2)] & 0x03;
                    bit23 = attr_table2[(y * 32) + (x * 2) + 1] & 0x03;
                    if(y != 7)
                    {
                        bit45 = attr_table2[(y * 32) + (x * 2) + 16] & 0x03;
                        bit67 = attr_table2[(y * 32) + (x * 2) + 17] & 0x03;
                    }
                    else
                    {
                        bit45 = 0;
                        bit67 = 0;
                    }
                    bit23 = bit23 << 2;
                    bit45 = bit45 << 4;
                    bit67 = bit67 << 6;
                    at_val = bit01 + bit23 + bit45 + bit67;
                    attr_table[at_offset] = at_val;
                    at_offset++;
                }
            }
        }

        private void saveNTAndATToolStripMenuItem_Click(object sender, EventArgs e)
        { // nametable and attribute table
            if ((has_loaded == 0) || (has_converted == 0))
            {
                MessageBox.Show("Image hasn't converted yet.");
                label3.Focus();
                return;
            }
            crunch_AT();

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Nametable (*.nam)|*.nam|DZ4 (*.dz4)|*.dz4";
            saveFileDialog1.Title = "Save the NT and AT";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile();

                out_size = 0;
                
                for (int i = 0; i < 960; i++)
                {
                    Out_Array[out_size] = nametable2[i];
                    out_size++;
                    
                }

                for (int i = 0; i < 64; i++)
                {
                    Out_Array[out_size] = attr_table[i];
                    out_size++;
                    
                }

                // now save out_array, is it compressed ?
                string ext = System.IO.Path.GetExtension(saveFileDialog1.FileName);
                if (ext == ".nam")
                {
                    for (int i = 0; i < out_size; i++)
                    {
                        fs.WriteByte((byte)Out_Array[i]);
                    }
                }
                else // compress dz4
                {
                    if (CompressIt(Out_Array, out_size) == 1)
                    {
                        // now in rle_array, rle_size
                        for (int i = 0; i < rle_size; i++)
                        {
                            fs.WriteByte(rle_array[i]);
                        }
                    }
                    else
                    {
                        //MessageBox.Show("Something went wrong?");
                        //should already be a message
                    }
                }

                fs.Close();
            }

            label3.Focus();
        }



        public void save_one_chr()
        {
            // Out_Array[65536] y*256 + x

            // divide image into 128x128 segments
            // left to right, top to bottom in 8x8 chunks
            // top pixels, divide index into 2 - 1 bit things
            // roll all the lower bits (0-7) and upper bits (8-15)
            // into 16 total bytes per 8x8 tile

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "chr File (*.chr)|*.chr|DZ4 (*.dz4)|*.dz4";
            saveFileDialog1.Title = "Save the CHR";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile();

                // do each 128x128 block separately
                // always do the top left 128x128
                out_size = 0;

                c_offset2 = 0;
                for (int y = 0; y < 128; y += 8)
                {
                    for (int x = 0; x < 128; x += 8)
                    {
                        // process each 8x8 tile separately
                        Dry_CHR_Loop(x, y);

                        // have all 16 bytes, save them to file
                        for (int i = 0; i < 16; i++)
                        {
                            Out_Array[out_size] = CHR_16bytes[i];
                            out_size++;
                            
                        }
                    }
                }
                // top right
                if (image_width > 128)
                {
                    c_offset2 = 128;

                    for (int y = 0; y < 128; y += 8)
                    {
                        for (int x = 0; x < 128; x += 8)
                        {
                            // process each 8x8 tile separately
                            Dry_CHR_Loop(x, y);

                            // have all 16 bytes, save them to file
                            for (int i = 0; i < 16; i++)
                            {
                                Out_Array[out_size] = CHR_16bytes[i];
                                out_size++;
                                
                            }
                        }
                    }
                }
                // bottom left
                if (image_height > 128)
                {
                    c_offset2 = 32768;

                    for (int y = 0; y < 128; y += 8)
                    {
                        for (int x = 0; x < 128; x += 8)
                        {
                            // process each 8x8 tile separately
                            Dry_CHR_Loop(x, y);

                            // have all 16 bytes, save them to file
                            for (int i = 0; i < 16; i++)
                            {
                                Out_Array[out_size] = CHR_16bytes[i];
                                out_size++;
                                
                            }
                        }
                    }
                }
                // bottom right
                if ((image_width > 128) && (image_height > 128))
                {
                    c_offset2 = 128 + 32768;

                    for (int y = 0; y < 128; y += 8)
                    {
                        for (int x = 0; x < 128; x += 8)
                        {
                            // process each 8x8 tile separately
                            Dry_CHR_Loop(x, y);

                            // have all 16 bytes, save them to file
                            for (int i = 0; i < 16; i++)
                            {
                                Out_Array[out_size] = CHR_16bytes[i];
                                out_size++;
                                
                            }
                        }
                    }
                }

                // now save out_array, is it compressed ?
                string ext = System.IO.Path.GetExtension(saveFileDialog1.FileName);
                if (ext == ".chr")
                {
                    for(int i = 0; i < out_size; i++)
                    {
                        fs.WriteByte((byte)Out_Array[i]);
                    }
                }
                else // compress dz4
                {
                    if(CompressIt(Out_Array, out_size) == 1)
                    {
                        // now in rle_array, rle_size
                        for(int i = 0; i < rle_size; i++)
                        {
                            fs.WriteByte(rle_array[i]);
                        }
                    }
                    else
                    {
                        //MessageBox.Show("Something went wrong?");
                        //should already be a message
                    }
                }

                fs.Close();
            }
        }


        public void Dry_CHR_Loop(int x, int y)
        { // common loop code
            // c_offset = CHR_Array offset
            // c_offset2 = 128x128 offset
            // Array Size = [65536]; // for CHR output

            int index0, index8, temp_bits, bit1, bit2, reorder = 0;
            index0 = 0;
            index8 = 8;
            bit1 = 0;
            bit2 = 0;
            for (int y2 = 0; y2 < 8; y2++)
            {
                for (int x2 = 0; x2 < 8; x2++)
                {
                    c_offset = ((y + y2) * 256) + x + x2 + c_offset2;

                    if (comboBox2.SelectedIndex == 1)
                    {
                        reorder = p1_CHR[c_offset];
                    }
                    else if (comboBox2.SelectedIndex == 2)
                    {
                        reorder = p2_CHR[c_offset];
                    }
                    else if (comboBox2.SelectedIndex == 3)
                    {
                        reorder = p3_CHR[c_offset];
                    }
                    else // 4
                    {
                        reorder = p4_CHR[c_offset];
                    }
                    bit1 = reorder & 1;
                    bit2 = reorder & 2;
                    bit2 = bit2 >> 1;

                    temp_bits = CHR_16bytes[index0];
                    temp_bits = ((temp_bits << 1) + bit1) & 0xff;
                    CHR_16bytes[index0] = temp_bits;
                    temp_bits = CHR_16bytes[index8];
                    temp_bits = ((temp_bits << 1) + bit2) & 0xff;
                    CHR_16bytes[index8] = temp_bits;
                }
                index0++;
                index8++;
            }
        }



        public void Dry_CHR_Loop2(int x, int y)
        { // common loop code
            // c_offset = CHR_Array offset
            // c_offset2 = 128x128 offset
            // Array Size = [65536]; // for CHR output

            int index0, index8, temp_bits, bit1, bit2, reorder = 0;
            index0 = 0;
            index8 = 8;
            bit1 = 0;
            bit2 = 0;
            for (int y2 = 0; y2 < 8; y2++)
            {
                for (int x2 = 0; x2 < 8; x2++)
                {
                    c_offset = ((y + y2) * 256) + x + x2 + c_offset2;

                    reorder = final_CHR[c_offset];

                    bit1 = reorder & 1;
                    bit2 = reorder & 2;
                    bit2 = bit2 >> 1;

                    temp_bits = CHR_16bytes[index0];
                    temp_bits = ((temp_bits << 1) + bit1) & 0xff;
                    CHR_16bytes[index0] = temp_bits;
                    temp_bits = CHR_16bytes[index8];
                    temp_bits = ((temp_bits << 1) + bit2) & 0xff;
                    CHR_16bytes[index8] = temp_bits;
                }
                index0++;
                index8++;
            }
        }



        private void saveNametableToolStripMenuItem_Click(object sender, EventArgs e)
        { 
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        { // app exit
            // close the program
            Application.Exit();
        }

        private void loadRGBToolStripMenuItem_Click(object sender, EventArgs e)
        { // load RGB palette
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Open palette RGB";
            openFileDialog1.Filter = "pal File (*.pal)|*.pal|All files (*.*)|*.*";

            int red, green, blue;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                System.IO.FileStream fs = (System.IO.FileStream)openFileDialog1.OpenFile();
                if (fs.Length == 48) //16*3
                {
                    int j = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        red = fs.ReadByte();
                        green = fs.ReadByte();
                        blue = fs.ReadByte();
                        if ((i == 4) || (i == 8) || (i == 12))
                        {
                            // skip the #0 color of each palette 1-3
                            continue;
                        }
                        col_array[j] = Color.FromArgb(red, green, blue);
                        ToNES(col_array[j], -1); // set remember index
                        col_val[j] = remember_index;
                        j++;
                    }

                    DRY_Palette(); // print numbers and color boxes
                }
                else
                {
                    MessageBox.Show("Error. Expected 48 byte file.");
                }

                fs.Close();
            }
            label3.Focus();
        }

        private void loadNESToolStripMenuItem_Click(object sender, EventArgs e)
        { // load NES palette, 16 bytes
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Open palette NES";
            openFileDialog1.Filter = "pal File (*.pal)|*.pal|All files (*.*)|*.*";

            int val1 = 0, red = 0, green = 0, blue = 0, index = 0;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                System.IO.FileStream fs = (System.IO.FileStream)openFileDialog1.OpenFile();
                if (fs.Length == 16)
                {
                    int j = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        val1 = fs.ReadByte();

                        if ((i == 4) || (i == 8) || (i == 12))
                        {
                            // skip the #0 color of each palette 1-3
                            continue;
                        }
                        col_val[j] = NEStoPaletteIndex(val1);
                        index = col_val[j] * 3;
                        red = NES_PALETTE[index];
                        index++;
                        green = NES_PALETTE[index];
                        index++;
                        blue = NES_PALETTE[index];
                        col_array[j] = Color.FromArgb(red, green, blue);
                        j++;
                    }

                    DRY_Palette(); // print numbers and color boxes
                }
                else
                {
                    MessageBox.Show("Error. Expected 16 byte file.");
                }

                fs.Close();
            }
            label3.Focus();
        }


        public int NEStoPaletteIndex(int inValue)
        {
            // I regret rearranging the palette colors.
            inValue = inValue & 0x3f; // force 0-63

            if (inValue < 0) return 0;
            if (inValue > 63) return 0;
            if (inValue == 0) return 13; // dark gray
            if (inValue < 13) return inValue;
            if (inValue < 16) return 0; // blacks
            if (inValue == 16) return 26; // light gray
            if (inValue < 29) return inValue - 3;
            if (inValue < 32) return 0; // blacks
            if (inValue == 32) return 39; // white
            if (inValue < 45) return inValue - 6;
            if (inValue == 45) return 13; // dark gray
            if (inValue < 48) return 0; // blacks
            if (inValue == 48) return 39; // white
            if (inValue < 61) return inValue - 9;
            if (inValue == 61) return 26; // light gray
            return 0; // blacks
        }


        public void DRY_Palette()
        { // print numbers and color boxes
            // convert all RGB to nearest NES color

            for (int i = 0; i < 13; i++)
            {
                col_array[i] = ToNES(col_array[i], -1);
                string nes_str = GetNesVal(remember_index);
                switch (i)
                {
                    case 0:
                        label7.Text = nes_str;
                        pictureBox4.BackColor = col_array[0];
                        break;
                    case 1:
                        label8.Text = nes_str;
                        pictureBox5.BackColor = col_array[1];
                        break;
                    case 2:
                        label9.Text = nes_str;
                        pictureBox6.BackColor = col_array[2];
                        break;
                    case 3:
                        label10.Text = nes_str;
                        pictureBox7.BackColor = col_array[3];
                        break;
                    case 4:
                        label15.Text = nes_str;
                        pictureBox8.BackColor = col_array[4];
                        break;
                    case 5:
                        label16.Text = nes_str;
                        pictureBox9.BackColor = col_array[5];
                        break;
                    case 6:
                        label17.Text = nes_str;
                        pictureBox10.BackColor = col_array[6];
                        break;
                    case 7:
                        label18.Text = nes_str;
                        pictureBox11.BackColor = col_array[7];
                        break;
                    case 8:
                        label19.Text = nes_str;
                        pictureBox12.BackColor = col_array[8];
                        break;
                    case 9:
                        label20.Text = nes_str;
                        pictureBox13.BackColor = col_array[9];
                        break;
                    case 10:
                        label21.Text = nes_str;
                        pictureBox14.BackColor = col_array[10];
                        break;
                    case 11:
                        label22.Text = nes_str;
                        pictureBox15.BackColor = col_array[11];
                        break;
                    case 12:
                        label23.Text = nes_str;
                        pictureBox16.BackColor = col_array[12];
                        break;
                    default:
                        break;
                }
            }

        }

        public void DRY_Palette2()
        { // print numbers and color boxes
            // convert all RGB to nearest NES color

            for (int i = 0; i < 6; i++)
            {
                col_array_extra[i] = ToNES(col_array_extra[i], -1);
                string nes_str = GetNesVal(remember_index);
                switch (i)
                {
                    case 0:
                        label2.Text = nes_str;
                        pictureBox19.BackColor = col_array_extra[0];
                        break;
                    case 1:
                        label12.Text = nes_str;
                        pictureBox18.BackColor = col_array_extra[1];
                        break;
                    case 2:
                        label27.Text = nes_str;
                        pictureBox17.BackColor = col_array_extra[2];
                        break;
                    case 3:
                        label28.Text = nes_str;
                        pictureBox22.BackColor = col_array_extra[3];
                        break;
                    case 4:
                        label29.Text = nes_str;
                        pictureBox21.BackColor = col_array_extra[4];
                        break;
                    case 5:
                        label30.Text = nes_str;
                        pictureBox20.BackColor = col_array_extra[5];
                        break;
                    
                    default:
                        break;
                }
            }

        }

        private void saveRGBToolStripMenuItem_Click(object sender, EventArgs e)
        { // save RGB palette
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "pal File (*.pal)|*.pal|All files (*.*)|*.*";
            saveFileDialog1.Title = "Save the palette RGB";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile();

                int j = 0;
                for (int i = 0; i < 16; i++)
                {
                    if ((i == 4) || (i == 8) || (i == 12))
                    {
                        fs.WriteByte(col_array[0].R);
                        fs.WriteByte(col_array[0].G);
                        fs.WriteByte(col_array[0].B);
                        continue;
                    }
                    fs.WriteByte(col_array[j].R);
                    fs.WriteByte(col_array[j].G);
                    fs.WriteByte(col_array[j].B);
                    j++;
                }

                fs.Close();
            }
            label3.Focus();
        }

        private void saveNESToolStripMenuItem_Click(object sender, EventArgs e)
        { // save NES palette, 16 byte
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "pal File (*.pal)|*.pal|All files (*.*)|*.*";
            saveFileDialog1.Title = "Save the palette RGB";
            saveFileDialog1.ShowDialog();

            int value_zero = 0, NES_val = 0;

            if (saveFileDialog1.FileName != "")
            {
                System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile();

                value_zero = Pal_to_NES(col_val[0]);

                int j = 0;
                for (int i = 0; i < 16; i++)
                {
                    if ((i == 4) || (i == 8) || (i == 12))
                    {
                        // use the #0 color for all palettes
                        fs.WriteByte((byte)value_zero);
                        continue;
                    }
                    NES_val = Pal_to_NES(col_val[j]);
                    fs.WriteByte((byte)NES_val);
                    j++;
                }

                fs.Close();
            }
            label3.Focus();
        }


        public int Pal_to_NES(int index)
        {
            // the index omits the xD, xE, xF colors
            // so the upper values are progressively off by 3
            if (index < 0) return 15; // 15 is black
            if (index > 51) return 15;
            if (index == 0) return 15; // black
            if (index < 13) return index;
            if (index == 13) return 0; // dark gray
            if (index < 26) return index + 3;
            if (index == 26) return 16; // light gray
            if (index < 39) return index + 6;
            if (index == 39) return 48; // white
            if (index < 52) return index + 9;
            return 0;
        }


        private void nESToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        { // copy palette to clipboard as text
            string out_str = "";

            out_str = label7.Text;
            out_str += ", ";
            out_str += label8.Text;
            out_str += ", ";
            out_str += label9.Text;
            out_str += ", ";
            out_str += label10.Text;

            out_str += ", ";
            out_str += label7.Text;
            out_str += ", ";
            out_str += label15.Text;
            out_str += ", ";
            out_str += label16.Text;
            out_str += ", ";
            out_str += label17.Text;

            out_str += ", ";
            out_str += label7.Text;
            out_str += ", ";
            out_str += label18.Text;
            out_str += ", ";
            out_str += label19.Text;
            out_str += ", ";
            out_str += label20.Text;

            out_str += ", ";
            out_str += label7.Text;
            out_str += ", ";
            out_str += label21.Text;
            out_str += ", ";
            out_str += label22.Text;
            out_str += ", ";
            out_str += label23.Text;

            if (out_str != "")
            {
                Clipboard.SetDataObject(out_str);
            }
            label3.Focus();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("2021 Doug Fraker.\nnesdoug.com");
            label3.Focus();
        }

        

        private void pictureBox2_Click(object sender, EventArgs e)
        { // the NES palette box
            // grab a color from it
            var mouseEventArgs = e as MouseEventArgs;
            if (mouseEventArgs == null)
            {
                label3.Focus();
                return;
            }
            int map_x = mouseEventArgs.X;
            int map_y = mouseEventArgs.Y;
            if ((map_x < 0) || (map_x >= 208) ||
                (map_y < 0) || (map_y >= 64))
            {
                label3.Focus();
                return;
            }

            map_x = map_x >> 4;
            map_y = map_y >> 4;
            int final_color = (map_y * 13) + map_x;
            int index = final_color * 3;
            int final_r = NES_PALETTE[index];
            index++;
            int final_g = NES_PALETTE[index];
            index++;
            int final_b = NES_PALETTE[index];

            remember_index = final_color;
            string nes_str = GetNesVal(remember_index);
            label5.Text = nes_str;

            sel_color = Color.FromArgb(final_r, final_g, final_b);
            pictureBox3.BackColor = sel_color;
            sel_color_val = remember_index;

            label3.Focus();
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        { // color #0, main BG
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[0];
                sel_color = col_array[0];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label7.Text;
            }
            else // left click
            {
                col_val[0] = sel_color_val;
                col_array[0] = sel_color;
                pictureBox4.BackColor = sel_color;
                label7.Text = label5.Text;
            }

            label3.Focus();
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        { // pal #0, color #1
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[1];
                sel_color = col_array[1];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label8.Text;
            }
            else // left click
            {
                col_val[1] = sel_color_val;
                col_array[1] = sel_color;
                pictureBox5.BackColor = sel_color;
                label8.Text = label5.Text;
            }

            label3.Focus();
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        { // pal #0, color #2
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[2];
                sel_color = col_array[2];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label9.Text;
            }
            else // left click
            {
                col_val[2] = sel_color_val;
                col_array[2] = sel_color;
                pictureBox6.BackColor = sel_color;
                label9.Text = label5.Text;
            }

            label3.Focus();
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        { // pal #0, color #3
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[3];
                sel_color = col_array[3];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label10.Text;
            }
            else // left click
            {
                col_val[3] = sel_color_val;
                col_array[3] = sel_color;
                pictureBox7.BackColor = sel_color;
                label10.Text = label5.Text;
            }
            
            label3.Focus();
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        { // pal #1, color #1
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[4];
                sel_color = col_array[4];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label15.Text;
            }
            else // left click
            {
                col_val[4] = sel_color_val;
                col_array[4] = sel_color;
                pictureBox8.BackColor = sel_color;
                label15.Text = label5.Text;
            }
            
            label3.Focus();
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        { // pal #1, color #2
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[5];
                sel_color = col_array[5];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label16.Text;
            }
            else // left click
            {
                col_val[5] = sel_color_val;
                col_array[5] = sel_color;
                pictureBox9.BackColor = sel_color;
                label16.Text = label5.Text;
            }
            
            label3.Focus();
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        { // pal #1, color #3
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[6];
                sel_color = col_array[6];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label17.Text;
            }
            else // left click
            {
                col_val[6] = sel_color_val;
                col_array[6] = sel_color;
                pictureBox10.BackColor = sel_color;
                label17.Text = label5.Text;
            }
            
            label3.Focus();
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        { // pal #2, color #1
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[7];
                sel_color = col_array[7];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label18.Text;
            }
            else // left click
            {
                col_val[7] = sel_color_val;
                col_array[7] = sel_color;
                pictureBox11.BackColor = sel_color;
                label18.Text = label5.Text;
            }
            
            label3.Focus();
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        { // pal #2, color #2
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[8];
                sel_color = col_array[8];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label19.Text;
            }
            else // left click
            {
                col_val[8] = sel_color_val;
                col_array[8] = sel_color;
                pictureBox12.BackColor = sel_color;
                label19.Text = label5.Text;
            }
            
            label3.Focus();
        }

        private void pictureBox13_Click(object sender, EventArgs e)
        { // pal #2, color #3
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[9];
                sel_color = col_array[9];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label20.Text;
            }
            else // left click
            {
                col_val[9] = sel_color_val;
                col_array[9] = sel_color;
                pictureBox13.BackColor = sel_color;
                label20.Text = label5.Text;
            }
            
            label3.Focus();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        { // click on the picture, get the color -> selected color
          // if converted, it will set the palette for 16x16 box
            if (has_loaded == 0)
            {
                label3.Focus();
                return;
            }
            var mouseEventArgs = e as MouseEventArgs;
            if (mouseEventArgs == null)
            {
                label3.Focus();
                return;
            }

            int map_x = mouseEventArgs.X;
            int map_y = mouseEventArgs.Y;

            if ((map_x < 0) || (map_x >= image_width) ||
                (map_y < 0) || (map_y >= image_height))
            {
                label3.Focus();
                return;
            }


            if(has_converted == 0)
            { // grab a color from the image
                Color tempcolor = left_bmp.GetPixel(map_x, map_y);

                tempcolor = ToNES(tempcolor, -1);
                // ToNES also sets remember_index

                sel_color = tempcolor;
                pictureBox3.BackColor = sel_color;
                sel_color_val = remember_index;

                string nes_str = GetNesVal(remember_index);
                label5.Text = nes_str;
            }
            else
            { // set a palette for a 16x16 slot, then redraw the image
                if(mouseEventArgs.Button == MouseButtons.Left)
                {
                    comboBox2.SelectedIndex = 0;
                    int which_pal = comboBox3.SelectedIndex;

                    if (lt_click_mode == 1) // rotate palette instead
                    {
                        which_pal = attr_table2[(map_y & 0xf0) + (map_x / 16)];
                        which_pal++;
                        if (which_pal > 3) which_pal = 0;
                    }

                    attr_table2[(map_y & 0xf0) + (map_x / 16)] = which_pal;
                    //16 * 15 = 240

                    Color temp_color = Color.Black;

                    // copy just the 16x16 box
                    int start_x = map_x & 0xf0;
                    int start_y = map_y & 0xf0;

                    for (int xx = 0; xx < 16; xx++)
                    {
                        for (int yy = 0; yy < 16; yy++)
                        {
                            int combo_x = xx + start_x;
                            int combo_y = yy + start_y;
                            int chr_val = 0;
                            chr_index = (combo_y * 256) + combo_x;

                            if (which_pal == 0)
                            {
                                temp_color = p1_bmp.GetPixel(combo_x, combo_y);
                                chr_val = p1_CHR[chr_index];
                            }
                            else if (which_pal == 1)
                            {
                                temp_color = p2_bmp.GetPixel(combo_x, combo_y);
                                chr_val = p2_CHR[chr_index];
                            }
                            else if (which_pal == 2)
                            {
                                temp_color = p3_bmp.GetPixel(combo_x, combo_y);
                                chr_val = p3_CHR[chr_index];
                            }
                            else // which_pal == 3
                            {
                                temp_color = p4_bmp.GetPixel(combo_x, combo_y);
                                chr_val = p4_CHR[chr_index];
                            }
                            all_bmp.SetPixel(combo_x, combo_y, temp_color);

                            // also copy the CHR value
                            all_CHR[chr_index] = chr_val;
                        }
                    }


                    // copy pixel by pixel
                    for (int xx = 0; xx < MAX_WIDTH; xx++)
                    {
                        for (int yy = 0; yy < MAX_HEIGHT; yy++)
                        {
                            if ((xx < image_width) && (yy < image_height))
                            {
                                temp_color = all_bmp.GetPixel(xx, yy);
                            }
                            else
                            {
                                temp_color = Color.Gray;
                            }
                            left_bmp.SetPixel(xx, yy, temp_color);
                        }
                    }

                    pictureBox1.Image = left_bmp;
                    pictureBox1.Refresh();

                    shrink_CHR(); // and make a nametable
                } // end of if click == left button

                if (mouseEventArgs.Button == MouseButtons.Right)
                {
                    // get the palette instead, and change the dropdown
                    int which_pal = attr_table2[(map_y & 0xf0) + (map_x / 16)];
                    comboBox3.SelectedIndex = which_pal;
                }
            }
            
            label3.Focus();
        }



        public Color ToNES(Color tempcolor, int forbid)
        {
            // forbid is optional index to not allow.
            forbid = forbid * 3;
            // return closest color in the NES palette
            // 13 x 4 = 52 colors x 3 = 156 byte array
            int color_diff = 0, best_index = 0, lowest_diff = 999999;
            int dR = 0, dG = 0, dB = 0;
            int rr = 0;
            int gg = 1;
            int bb = 2;
            // check every NES color, which is closest match?
            for (; rr < 156; rr += 3, gg += 3, bb += 3)
            {
                if (rr == forbid) continue;
                dR = tempcolor.R - NES_PALETTE[rr];
                dG = tempcolor.G - NES_PALETTE[gg];
                dB = tempcolor.B - NES_PALETTE[bb];
                // note, the formula is supposed to take the Math.Sqrt()
                // of this but that step has been removed as unneeded.
                color_diff = ((dR * dR) + (dG * dG) + (dB * dB));

                if (color_diff < lowest_diff)
                {
                    lowest_diff = color_diff;
                    best_index = rr;
                }
            }

            rr = best_index;
            gg = best_index + 1;
            bb = best_index + 2;

            tempcolor = Color.FromArgb(NES_PALETTE[rr], NES_PALETTE[gg], NES_PALETTE[bb]);

            remember_index = best_index / 3; // pass to global

            return tempcolor;
        }


        private void pictureBox14_Click(object sender, EventArgs e)
        { // pal #3, color #1
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[10];
                sel_color = col_array[10];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label21.Text;
            }
            else // left click
            {
                col_val[10] = sel_color_val;
                col_array[10] = sel_color;
                pictureBox14.BackColor = sel_color;
                label21.Text = label5.Text;
            }
            
            label3.Focus();
        }

        private void label3_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        { // check if Ctrl+V is pressed
            // lots of label3.Focus() to direct button presses here
            // prehaps some of them are unneeded ?

            if (e.KeyCode == Keys.V)
            {
                paste_clipboard();
                label3.Focus();
            }

        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            label3.Focus();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            label3.Focus();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        { // change which is shown
            if ((has_loaded == 0) || (has_converted == 0) || (skip_combo != 0))
            {
                label3.Focus();
                return;
            }
            
            Color temp_color = Color.Black;

            if (comboBox2.SelectedIndex == 0) // all 4
            {
                // copy pixel by pixel
                for (int xx = 0; xx < MAX_WIDTH; xx++)
                {
                    for (int yy = 0; yy < MAX_HEIGHT; yy++)
                    {
                        if ((xx < image_width) && (yy < image_height))
                        {
                            temp_color = all_bmp.GetPixel(xx, yy);
                        }
                        else
                        {
                            temp_color = Color.Gray;
                        }
                        left_bmp.SetPixel(xx, yy, temp_color);
                    }
                }
            }
            else if (comboBox2.SelectedIndex == 1)
            {
                comboBox3.SelectedIndex = 0; // pal 1
                
                // copy pixel by pixel
                for (int xx = 0; xx < MAX_WIDTH; xx++)
                {
                    for (int yy = 0; yy < MAX_HEIGHT; yy++)
                    {
                        if ((xx < image_width) && (yy < image_height))
                        {
                            temp_color = p1_bmp.GetPixel(xx, yy);
                        }
                        else
                        {
                            temp_color = Color.Gray;
                        }
                        left_bmp.SetPixel(xx, yy, temp_color);
                    }
                }
            }
            else if (comboBox2.SelectedIndex == 2)
            {
                comboBox3.SelectedIndex = 1; // pal 2

                // copy pixel by pixel
                for (int xx = 0; xx < MAX_WIDTH; xx++)
                {
                    for (int yy = 0; yy < MAX_HEIGHT; yy++)
                    {
                        if ((xx < image_width) && (yy < image_height))
                        {
                            temp_color = p2_bmp.GetPixel(xx, yy);
                        }
                        else
                        {
                            temp_color = Color.Gray;
                        }
                        left_bmp.SetPixel(xx, yy, temp_color);
                    }
                }
            }
            else if (comboBox2.SelectedIndex == 3)
            {
                comboBox3.SelectedIndex = 2; // pal 3

                // copy pixel by pixel
                for (int xx = 0; xx < MAX_WIDTH; xx++)
                {
                    for (int yy = 0; yy < MAX_HEIGHT; yy++)
                    {
                        if ((xx < image_width) && (yy < image_height))
                        {
                            temp_color = p3_bmp.GetPixel(xx, yy);
                        }
                        else
                        {
                            temp_color = Color.Gray;
                        }
                        left_bmp.SetPixel(xx, yy, temp_color);
                    }
                }
            }
            else if (comboBox2.SelectedIndex == 4)
            {
                comboBox3.SelectedIndex = 3; // pal 4

                // copy pixel by pixel
                for (int xx = 0; xx < MAX_WIDTH; xx++)
                {
                    for (int yy = 0; yy < MAX_HEIGHT; yy++)
                    {
                        if ((xx < image_width) && (yy < image_height))
                        {
                            temp_color = p4_bmp.GetPixel(xx, yy);
                        }
                        else
                        {
                            temp_color = Color.Gray;
                        }
                        left_bmp.SetPixel(xx, yy, temp_color);
                    }
                }
            }


            // show in picture box
            pictureBox1.Image = left_bmp;
            pictureBox1.Refresh();


            label3.Focus();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        { // dither factor
            if (e.KeyChar == (char)Keys.Return)
            {
                dither_factor_set();

                e.Handled = true; // prevent ding on return press

                label3.Focus();
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        { // dither factor
            dither_factor_set();
            label3.Focus();
        }


        public void dither_factor_set()
        {
            string str = textBox1.Text;
            int outvar = 0;
            if (int.TryParse(str, out outvar))
            {
                if (outvar > 12) outvar = 12;
                if (outvar < 0) outvar = 0;
                dither_factor = outvar;
                textBox1.Text = outvar.ToString();
            }
            else
            {
                // revert back to previous
                textBox1.Text = dither_factor.ToString();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        { // convert button
            if (has_loaded == 0)
            {
                label3.Focus();
                return;
            }

            if((pal1_on == false) &&
                (pal2_on == false) &&
                (pal3_on == false) &&
                (pal4_on == false))
            {
                MessageBox.Show("Error. All palettes are turned off.");
                label3.Focus();
                return;
            }

            // clear all CHR arrays
            for(int i = 0; i < 65536; i++)
            {
                p1_CHR[i] = 0;
                p2_CHR[i] = 0;
                p3_CHR[i] = 0;
                p4_CHR[i] = 0;
                all_CHR[i] = 0;
            } //chr_index

            int red, green, blue, bayer_val;
            int red_dif, green_dif, blue_dif;

            Color tempcolor = Color.Black;
            Color tempcolor2 = Color.Black;
            dither_db = dither_factor / 10.0;
            dither_adjust = (int)(dither_db * 32.0);

            //copy orig to right_bmp, dither on it.
            if (comboBox1.SelectedIndex == FLOYD_STEIN)
            {
                //right_bmp
                for (int yy = 0; yy < image_height; yy++)
                {
                    for (int xx = 0; xx < image_width; xx++)
                    {
                        // do the dither later
                        tempcolor = bright_bmp.GetPixel(xx, yy);
                        scratch_bmp.SetPixel(xx, yy, tempcolor);
                    }
                }
            }
            else // BAYER8
            {
                // do the dither now
                for (int yy = 0; yy < image_height; yy++)
                {
                    for (int xx = 0; xx < image_width; xx++)
                    {
                        if (dither_factor > 0)
                        {
                            tempcolor = bright_bmp.GetPixel(xx, yy);
                            red = tempcolor.R - dither_adjust; // keep it from lightening
                            green = tempcolor.G - dither_adjust;
                            blue = tempcolor.B - dither_adjust;
                            bayer_val = BAYER_MATRIX[xx % 8, yy % 8];
                            bayer_val = (int)((double)bayer_val * dither_db);
                            red += bayer_val;
                            red = Clamp255(red);
                            green += bayer_val;
                            green = Clamp255(green);
                            blue += bayer_val;
                            blue = Clamp255(blue);
                            scratch_bmp.SetPixel(xx, yy, Color.FromArgb(red, green, blue));
                        }
                        else
                        { // no dither factor
                            tempcolor = bright_bmp.GetPixel(xx, yy);
                            red = tempcolor.R;
                            red = Clamp255(red);
                            green = tempcolor.G;
                            green = Clamp255(green);
                            blue = tempcolor.B;
                            blue = Clamp255(blue);
                            scratch_bmp.SetPixel(xx, yy, Color.FromArgb(red, green, blue));
                        }

                    }
                }
            }

            dither_db = dither_factor / 12.0; // 10 seemed too much


            // test 1st palette
            col_array4[0] = col_array[0];
            col_array4[1] = col_array[1];
            col_array4[2] = col_array[2];
            col_array4[3] = col_array[3];

            for(int y = 0; y < image_height; y++)
            {
                for(int x = 0; x < image_width; x++)
                {
                    tempcolor = scratch_bmp.GetPixel(x, y);
                    tempcolor2 = get_best_of_4(tempcolor);
                    p1_bmp.SetPixel(x, y, tempcolor2);
                    //get_best_of_4 also sets chr_index
                    p1_CHR[(y * 256) + x] = chr_index;

                    if ((comboBox1.SelectedIndex == FLOYD_STEIN) && (dither_factor != 0))
                    {
                        // do the dither stuff
                        red_dif = tempcolor.R - tempcolor2.R;
                        green_dif = tempcolor.G - tempcolor2.G;
                        blue_dif = tempcolor.B - tempcolor2.B;
                        do_dither(red_dif, green_dif, blue_dif, x, y);
                    }
                }
            }

            // set up the scratch_bmp again
            if (comboBox1.SelectedIndex == FLOYD_STEIN)
            {
                //right_bmp
                for (int yy = 0; yy < image_height; yy++)
                {
                    for (int xx = 0; xx < image_width; xx++)
                    {
                        // do the dither later
                        tempcolor = bright_bmp.GetPixel(xx, yy);
                        scratch_bmp.SetPixel(xx, yy, tempcolor);
                    }
                }
            }

            // test 2nd palette
            col_array4[1] = col_array[4];
            col_array4[2] = col_array[5];
            col_array4[3] = col_array[6];

            for (int y = 0; y < image_height; y++)
            {
                for (int x = 0; x < image_width; x++)
                {
                    tempcolor = scratch_bmp.GetPixel(x, y);
                    tempcolor2 = get_best_of_4(tempcolor);
                    p2_bmp.SetPixel(x, y, tempcolor2);
                    //get_best_of_4 also sets chr_index
                    p2_CHR[(y * 256) + x] = chr_index;

                    if ((comboBox1.SelectedIndex == FLOYD_STEIN) && (dither_factor != 0))
                    {
                        // do the dither stuff
                        red_dif = tempcolor.R - tempcolor2.R;
                        green_dif = tempcolor.G - tempcolor2.G;
                        blue_dif = tempcolor.B - tempcolor2.B;
                        do_dither(red_dif, green_dif, blue_dif, x, y);
                    }
                }
            }

            // set up the scratch_bmp again
            if (comboBox1.SelectedIndex == FLOYD_STEIN)
            {
                //right_bmp
                for (int yy = 0; yy < image_height; yy++)
                {
                    for (int xx = 0; xx < image_width; xx++)
                    {
                        // do the dither later
                        tempcolor = bright_bmp.GetPixel(xx, yy);
                        scratch_bmp.SetPixel(xx, yy, tempcolor);
                    }
                }
            }

            // test 3rd palette
            col_array4[1] = col_array[7];
            col_array4[2] = col_array[8];
            col_array4[3] = col_array[9];

            for (int y = 0; y < image_height; y++)
            {
                for (int x = 0; x < image_width; x++)
                {
                    tempcolor = scratch_bmp.GetPixel(x, y);
                    tempcolor2 = get_best_of_4(tempcolor);
                    p3_bmp.SetPixel(x, y, tempcolor2);
                    //get_best_of_4 also sets chr_index
                    p3_CHR[(y * 256) + x] = chr_index;

                    if ((comboBox1.SelectedIndex == FLOYD_STEIN) && (dither_factor != 0))
                    {
                        // do the dither stuff
                        red_dif = tempcolor.R - tempcolor2.R;
                        green_dif = tempcolor.G - tempcolor2.G;
                        blue_dif = tempcolor.B - tempcolor2.B;
                        do_dither(red_dif, green_dif, blue_dif, x, y);
                    }
                }
            }

            // set up the scratch_bmp again
            if (comboBox1.SelectedIndex == FLOYD_STEIN)
            {
                //right_bmp
                for (int yy = 0; yy < image_height; yy++)
                {
                    for (int xx = 0; xx < image_width; xx++)
                    {
                        // do the dither later
                        tempcolor = bright_bmp.GetPixel(xx, yy);
                        scratch_bmp.SetPixel(xx, yy, tempcolor);
                    }
                }
            }

            // test 4th palette
            col_array4[1] = col_array[10];
            col_array4[2] = col_array[11];
            col_array4[3] = col_array[12];

            for (int y = 0; y < image_height; y++)
            {
                for (int x = 0; x < image_width; x++)
                {
                    tempcolor = scratch_bmp.GetPixel(x, y);
                    tempcolor2 = get_best_of_4(tempcolor);
                    p4_bmp.SetPixel(x, y, tempcolor2);
                    //get_best_of_4 also sets chr_index
                    p4_CHR[(y * 256) + x] = chr_index;

                    if ((comboBox1.SelectedIndex == FLOYD_STEIN) && (dither_factor != 0))
                    {
                        // do the dither stuff
                        red_dif = tempcolor.R - tempcolor2.R;
                        green_dif = tempcolor.G - tempcolor2.G;
                        blue_dif = tempcolor.B - tempcolor2.B;
                        do_dither(red_dif, green_dif, blue_dif, x, y);
                    }
                }
            }

            // now find the best palette for each 16x16 square, and
            // mark them on attr_table2[], then copy from each to all_bmp

            int combo_y = 0;
            int combo_x = 0;

            int dR, dG, dB;
            tempcolor2 = Color.Black;

            for(int y2 = 0; y2 < 15; y2++)
            {
                for (int x2 = 0; x2 < 16; x2++)
                {
                    
                    int p1_diff = 0;
                    int p2_diff = 0;
                    int p3_diff = 0;
                    int p4_diff = 0;

                    for (int y = 0; y < 16; y++)
                    {
                        combo_y = (y2 * 16) + y;
                        if (combo_y >= image_height) break;
                        for (int x = 0; x < 16; x++)
                        {
                            combo_x = (x2 * 16) + x;
                            if (combo_x >= image_width) break;
                            tempcolor = scratch_bmp.GetPixel(combo_x, combo_y);
                            tempcolor2 = p1_bmp.GetPixel(combo_x, combo_y);
                            dR = tempcolor.R - tempcolor2.R;
                            dG = tempcolor.G - tempcolor2.G;
                            dB = tempcolor.B - tempcolor2.B;
                            diff_val = ((dR * dR) + (dG * dG) + (dB * dB));
                            p1_diff += diff_val;

                            tempcolor2 = p2_bmp.GetPixel(combo_x, combo_y);
                            dR = tempcolor.R - tempcolor2.R;
                            dG = tempcolor.G - tempcolor2.G;
                            dB = tempcolor.B - tempcolor2.B;
                            diff_val = ((dR * dR) + (dG * dG) + (dB * dB));
                            p2_diff += diff_val;

                            tempcolor2 = p3_bmp.GetPixel(combo_x, combo_y);
                            dR = tempcolor.R - tempcolor2.R;
                            dG = tempcolor.G - tempcolor2.G;
                            dB = tempcolor.B - tempcolor2.B;
                            diff_val = ((dR * dR) + (dG * dG) + (dB * dB));
                            p3_diff += diff_val;

                            tempcolor2 = p4_bmp.GetPixel(combo_x, combo_y);
                            dR = tempcolor.R - tempcolor2.R;
                            dG = tempcolor.G - tempcolor2.G;
                            dB = tempcolor.B - tempcolor2.B;
                            diff_val = ((dR * dR) + (dG * dG) + (dB * dB));
                            p4_diff += diff_val;
                        }
                    }

                    // just give these an absurd value if off
                    if(pal1_on == false)
                    {
                        //needs to be bigger than 50,000,000
                        p1_diff = 999999999;
                    }
                    if (pal2_on == false)
                    {
                        p2_diff = 999999999;
                    }
                    if (pal3_on == false)
                    {
                        p3_diff = 999999999;
                    }
                    if (pal4_on == false)
                    {
                        p4_diff = 999999999;
                    }

                    // now which was better?
                    int least_index = 0;
                    int least_diff = p1_diff;
                    if(p2_diff < least_diff)
                    {
                        least_diff = p2_diff;
                        least_index = 1;
                    }
                    if (p3_diff < least_diff)
                    {
                        least_diff = p3_diff;
                        least_index = 2;
                    }
                    if (p4_diff < least_diff)
                    {
                        //least_diff = p3_diff; // not needed
                        least_index = 3;
                    }
                    // mark it
                    attr_table2[(y2 * 16) + x2] = least_index;

                    // copy the pixels over
                    if(least_index == 0)
                    {
                        for (int y = 0; y < 16; y++)
                        {
                            combo_y = (y2 * 16) + y;
                            if (combo_y >= image_height) break;
                            for (int x = 0; x < 16; x++)
                            {
                                combo_x = (x2 * 16) + x;
                                if (combo_x >= image_width) break;
                                tempcolor = p1_bmp.GetPixel(combo_x, combo_y);
                                all_bmp.SetPixel(combo_x, combo_y, tempcolor);
                                chr_index = (combo_y * 256) + combo_x;
                                all_CHR[chr_index] = p1_CHR[chr_index];
                            }
                        }
                    }
                    else if(least_index == 1)
                    {
                        for (int y = 0; y < 16; y++)
                        {
                            combo_y = (y2 * 16) + y;
                            if (combo_y >= image_height) break;
                            for (int x = 0; x < 16; x++)
                            {
                                combo_x = (x2 * 16) + x;
                                if (combo_x >= image_width) break;
                                tempcolor = p2_bmp.GetPixel(combo_x, combo_y);
                                all_bmp.SetPixel(combo_x, combo_y, tempcolor);
                                chr_index = (combo_y * 256) + combo_x;
                                all_CHR[chr_index] = p2_CHR[chr_index];
                            }
                        }
                    }
                    else if(least_index == 2)
                    {
                        for (int y = 0; y < 16; y++)
                        {
                            combo_y = (y2 * 16) + y;
                            if (combo_y >= image_height) break;
                            for (int x = 0; x < 16; x++)
                            {
                                combo_x = (x2 * 16) + x;
                                if (combo_x >= image_width) break;
                                tempcolor = p3_bmp.GetPixel(combo_x, combo_y);
                                all_bmp.SetPixel(combo_x, combo_y, tempcolor);
                                chr_index = (combo_y * 256) + combo_x;
                                all_CHR[chr_index] = p3_CHR[chr_index];
                            }
                        }
                    }
                    else
                    {
                        for (int y = 0; y < 16; y++)
                        {
                            combo_y = (y2 * 16) + y;
                            if (combo_y >= image_height) break;
                            for (int x = 0; x < 16; x++)
                            {
                                combo_x = (x2 * 16) + x;
                                if (combo_x >= image_width) break;
                                tempcolor = p4_bmp.GetPixel(combo_x, combo_y);
                                all_bmp.SetPixel(combo_x, combo_y, tempcolor);
                                chr_index = (combo_y * 256) + combo_x;
                                all_CHR[chr_index] = p4_CHR[chr_index];
                            }
                        }
                    }
                }
            }
            // the tiles have been copied from the best palette to all_CHR



            shrink_CHR(); // and generate a nametable


            skip_combo = 1;
            has_converted = 1;
            comboBox2.SelectedIndex = 0; // set right

            Color temp_color = Color.Black;

            // copy pixel by pixel
            for (int xx = 0; xx < MAX_WIDTH; xx++)
            {
                for (int yy = 0; yy < MAX_HEIGHT; yy++)
                {
                    if ((xx < image_width) && (yy < image_height))
                    {
                        temp_color = all_bmp.GetPixel(xx, yy);
                    }
                    else
                    {
                        temp_color = Color.Gray;
                    }
                    left_bmp.SetPixel(xx, yy, temp_color);
                }
            }

            pictureBox1.Image = left_bmp;
            pictureBox1.Refresh();
            skip_combo = 0;
            label3.Focus();
        }



        public void shrink_CHR()
        { // and generate a nametable
            // blank nametable
            for (int i = 0; i < 960; i++)
            {
                nametable[i] = 0;
                nametable2[i] = 0;
            }

            // now make a nametable and remove duplicate tiles
            // and copy tiles over to a final_CHR[]
            // and count the num_tiles
            // round down to nearest tile size
            // ?? we could have rounded up, but, we didn't.
            image_width8 = image_width & 0x1f8;
            if (image_width8 < 8) image_width8 = 8;
            image_height8 = image_height & 0x1f8;
            if (image_height8 < 8) image_height8 = 8;

            int nt_index = 0; // bigger
            int nt_index2 = 0; // smaller, doesn't inc on duplicate
            int xy_index = 0;
            int xy_index2 = 0;
            int duplicate_val = 0;
            for (int y = 0; y < image_height8; y += 8)
            {
                for (int x = 0; x < image_width8; x += 8)
                {
                    if (nt_index == 0)
                    {
                        // always copy the first one
                        for (int y2 = 0; y2 < 8; y2++)
                        {
                            for (int x2 = 0; x2 < 8; x2++)
                            {
                                xy_index = (y2 * 256) + x2;
                                // treat the final_CHR as a 256x256 tileset
                                final_CHR[xy_index] = all_CHR[xy_index];
                            }
                        }
                        nametable[0] = 0;
                        nt_index++; // 1
                        nt_index2++; // 1

                    }
                    else
                    {
                        // see if it's a duplicate of previous tile
                        // check every tile in final_CHR[]
                        bool is_duplicate = false;
                        for (int i = 0; i < nt_index2; i++)
                        {
                            int y3 = (i / 32) * 8;
                            int x3 = (i % 32) * 8; // 32 tiles wide 8x8 = 256x256

                            for (int y2 = 0; y2 < 8; y2++)
                            {
                                for (int x2 = 0; x2 < 8; x2++)
                                {
                                    xy_index = ((y2 + y) * 256) + x2 + x; // current tile

                                    xy_index2 = ((y2 + y3) * 256) + x2 + x3; // final tiles

                                    if (all_CHR[xy_index] != final_CHR[xy_index2])
                                    {
                                        is_duplicate = false;
                                        goto Next;
                                    }
                                }
                            }
                            // if we made it here, we found a duplicate
                            is_duplicate = true;
                            duplicate_val = i;
                            break;

                        Next:
                            ;
                        }

                        if (is_duplicate == true)
                        {
                            // copy the duplicate's value to nametable
                            nametable[nt_index] = duplicate_val;
                            // but don't copy tiles
                            // don't inc nt_index2
                        }
                        else
                        {   // found unique tile
                            // copy the new tile to final_CHR[]
                            int y4 = (nt_index2 / 32) * 8;
                            int x4 = (nt_index2 % 32) * 8;

                            for (int y2 = 0; y2 < 8; y2++)
                            {
                                for (int x2 = 0; x2 < 8; x2++)
                                {
                                    xy_index = ((y2 + y) * 256) + x2 + x; // current tile

                                    xy_index2 = ((y2 + y4) * 256) + x2 + x4; // final destination

                                    final_CHR[xy_index2] = all_CHR[xy_index];
                                }
                            }

                            nametable[nt_index] = nt_index2;
                            nt_index2++;
                        }

                        nt_index++; // always inc
                    }

                }
            }


            // remember the nametable size for later. one larger than last index.
            nt_size = nt_index2;
            string str = "Tiles ";
            str += nt_size.ToString();
            label14.Text = str;


            // nametable is wrong... contiguous and not fit to the image
            // copy it to nametable2
            int counter = 0;
            int tile_width = image_width8 / 8;
            int tile_height = image_height8 / 8;
            for (int y5 = 0; y5 < tile_height; y5++)
            {
                for (int x5 = 0; x5 < tile_width; x5++)
                {
                    nametable2[(32 * y5) + x5] = nametable[counter];
                    counter++;
                }
            }
        }


        public Color get_best_of_4 (Color tempcolor)
        { //chr_index also needed
            int dR, dG, dB, best_index = 0;
            int best_val = 999999;

            r_val = tempcolor.R;
            g_val = tempcolor.G;
            b_val = tempcolor.B;

            for(int i = 0; i < 4; i++)
            {
                dR = r_val - col_array4[i].R;
                dG = g_val - col_array4[i].G;
                dB = b_val - col_array4[i].B;
                diff_val = ((dR * dR) + (dG * dG) + (dB * dB));

                if(diff_val < best_val)
                {
                    best_val = diff_val;
                    best_index = i;
                }
            }
            chr_index = best_index;
            return col_array4[best_index];
        }

        private void button2_Click(object sender, EventArgs e)
        { // revert button
            if(has_loaded == 0)
            {
                label3.Focus();
                return;
            }
            if (has_converted == 0)
            {
                label3.Focus();
                return;
            }


            Color temp_color = Color.Black;

            // copy pixel by pixel
            for (int xx = 0; xx < MAX_WIDTH; xx++)
            {
                for (int yy = 0; yy < MAX_HEIGHT; yy++)
                {
                    if ((xx < image_width) && (yy < image_height))
                    {
                        temp_color = bright_bmp.GetPixel(xx, yy);
                    }
                    else
                    {
                        temp_color = Color.Gray;
                    }
                    left_bmp.SetPixel(xx, yy, temp_color);
                }
            }


            // show in picture box
            pictureBox1.Image = left_bmp;
            pictureBox1.Refresh();
            label14.Text = "Tiles 0";

            has_converted = 0;
            label3.Focus();
        }














// ##################################################################
// ########################  big kahuna  ############################
// ##################################################################







        private void button3_Click(object sender, EventArgs e)
        { // auto generate palette
            if (has_loaded == 0)
            {
                label3.Focus();
                return;
            }

            int times3;

            // default colors
            for (int i = 0; i < 13; i++)
            {
                //col_array[i] = Color.Black;
                nes_array[i] = 0;
                final_array[i] = 0;
            }
            for (int i = 0; i < 6; i++)
            {
                extra_array[i] = 0;
            }

            // blank the arrays
            for (int i = 0; i < 52; i++)
            {
                Count_Array[i] = 0;
            }
            color_count = 0;
            Color tempcolor = Color.Black;

            // count all NES colors
            for (int yy = 0; yy < image_height; yy++)
            {
                for (int xx = 0; xx < image_width; xx++)
                {
                    tempcolor = bright_bmp.GetPixel(xx, yy);

                    tempcolor = ToNES(tempcolor, -1);
                    // also sets remember_index

                    Count_Array[remember_index] += 1;
                }
            }

            color_count = 0;
            // how many different colors
            for (int i = 0; i < 52; i++)
            {
                if (Count_Array[i] != 0) color_count++;
            }


            // then reduce to 13 colors, using a plain merge
            // the closest neighbor color

            color_count2 = color_count;
            while (color_count2 > 13)
            {
                //find the least count
                int least_index = 0;
                int least_cnt = 999999;
                for (int i = 0; i < 52; i++)
                {
                    if (Count_Array[i] == 0) continue;
                    if (Count_Array[i] < least_cnt)
                    {
                        least_cnt = Count_Array[i];
                        least_index = i;
                    }
                }
                // delete itself
                Count_Array[least_index] = 0;

                int closest_index = 0;
                int closest_val = 999999;
                times3 = least_index * 3;
                r_val = NES_PALETTE[times3];
                g_val = NES_PALETTE[times3 + 1];
                b_val = NES_PALETTE[times3 + 2];
                int dR = 0, dG = 0, dB = 0;

                // find the closest to that one
                for (int i = 0; i < 52; i++)
                {
                    if (Count_Array[i] == 0) continue;
                    times3 = i * 3;
                    dR = r_val - NES_PALETTE[times3];
                    dG = g_val - NES_PALETTE[times3 + 1];
                    dB = b_val - NES_PALETTE[times3 + 2];
                    diff_val = ((dR * dR) + (dG * dG) + (dB * dB));

                    if (diff_val < closest_val)
                    {
                        closest_val = diff_val;
                        closest_index = i;
                    }
                }
                

                Count_Array[closest_index] += least_cnt;

                color_count2--;

            }
            
            // find the 13 colors

            int final_index = 0;
            
            // print the colors at the bottom right
            string str = "";
            for (int i = 0; i < 52; i++)
            {
                if (Count_Array[i] == 0) continue;
                
                str += GetNesVal(i);
                str += " ";
                
                nes_array[final_index] = i;
                final_index++;
            }
            
            label36.Text = str;


            // now select the main bg color = #0
            // then add to forbid list
            // then find 2nd most = #1


            // blank the arrays
            for (int i = 0; i < 13; i++)
            {
                final_array[i] = -1;
                cnt_array[i] = 0;
            }

            // convert original BMP to 13 NES colors (or less)
            for (int y = 0; y < image_height; y++)
            {
                for (int x = 0; x < image_width; x++)
                {
                    int dR = 0, dG = 0, dB = 0;
                    int closest_index = 0;
                    int closest_val = 999999;
                    tempcolor = bright_bmp.GetPixel(x, y);
                    for (int i = 0; i < color_count2; i++)
                    { // find best
                        times3 = nes_array[i] * 3;
                        dR = tempcolor.R - NES_PALETTE[times3];
                        dG = tempcolor.G - NES_PALETTE[times3 + 1];
                        dB = tempcolor.B - NES_PALETTE[times3 + 2];
                        diff_val = ((dR * dR) + (dG * dG) + (dB * dB));

                        if (diff_val < closest_val)
                        {
                            closest_val = diff_val;
                            closest_index = i;
                        }
                    }

                    cnt_array[closest_index] += 1;
                    times3 = nes_array[closest_index] * 3;
                    r_val = NES_PALETTE[times3];
                    g_val = NES_PALETTE[times3 + 1];
                    b_val = NES_PALETTE[times3 + 2];
                    NES13_bmp.SetPixel(x, y, Color.FromArgb(r_val, g_val, b_val));

                    int sum = (y * 256) + x;
                    BMP_as_val[sum] = nes_array[closest_index]; // remember for later
                }
            }


            // sort nes array by count, which are most common color?
            Array.Sort(cnt_array, nes_array);
            // 12 = most, 11 = next, etc. 0 = least


            if(override_cb == true)
            {
                // we designated a specific color for universal color
                // tempcolor
                // do nothing if it already is that
                if (over_val != nes_array[12])
                {
                    // check if it exists on the list
                    int exists = 0;
                    for(int i = 11; i >= 0; i--)
                    {
                        if(nes_array[i] == over_val)
                        {
                            // found it, shift all above here down one
                            for(int j = i; j < 12; j++)
                            {
                                nes_array[j] = nes_array[j + 1];
                            }
                            nes_array[12] = over_val;
                            exists = 1;
                            break;
                        }
                    }
                    
                    if(exists == 0) // we didn't find it
                    {
                        //rotate all colors down 1 slot
                        for (int i = 0; i < 12; i++)
                        {
                            nes_array[i] = nes_array[i + 1];
                        }
                        nes_array[12] = over_val; // override universal color
                        
                        if (color_count2 < 13) color_count2++;
                    }
                    
                }
            }


            // we have the top color
            final_array[0] = nes_array[12]; // universal color
            

            // convert each to a color
            for (int i = 0; i < 13; i++)
            {
                times3 = nes_array[i] * 3;
                r_val = NES_PALETTE[times3];
                g_val = NES_PALETTE[times3 + 1];
                b_val = NES_PALETTE[times3 + 2];
                col_array[i] = Color.FromArgb(r_val, g_val, b_val);
            }
            // this col_array cooresponds to the nes_array
            // remember, 12 is the most common



            if(color_count2 <= 4)
            {
                // just use 1 palette and skip the rest
                final_array[1] = nes_array[11];
                final_array[2] = nes_array[10];
                final_array[3] = nes_array[9];

                Missing_Num();
                NES_to_Color();
                NES_to_Color2(); // extra array
                DRY_Palette(); // don't repeat yourself
                DRY_Palette2(); // extra array

                label3.Focus();

                return;
            }



            // blank each palette

            for (int i = 0; i < 72; i++)
            {
                Temp_Palettes[i] = -1;
                Final_Palettes[i] = -1;
                Count_Palettes[i] = 0;
            }
            for (int i = 0; i < 144; i++)
            {
                Ar_12x12[i] = 0;
            }

            // how many blocks of 16 to sort through
            int num_blocks_x = image_width / 16;
            int num_blocks_y = image_height / 16;

            // the image should be at least 16x16, but double checking anyway
            if (num_blocks_x < 1) num_blocks_x = 1;
            if (num_blocks_y < 1) num_blocks_y = 1;
            // note, the image should be a multiple of 16, or it will skip the last

            // go through each 16x16 block and count each color

            palette_index = 0;
            count_palettes = 0;

            for (int y2 = 0; y2 < num_blocks_y; y2++)
            {
                for (int x2 = 0; x2 < num_blocks_x; x2++)
                {
                    int start_y = 16 * y2;
                    int start_x = 16 * x2;

                    //blank each count, skip the 13th (universal color)
                    for(int i = 0; i < 12; i++)
                    {
                        cnt_array[i] = 0;
                    }

                    // inner loop for each block
                    for (int y = 0; y < 16; y++)
                    {
                        for (int x = 0; x < 16; x++)
                        {
                            // count each color in the block
                            // what color are we looking at?
                            int final_y = y + start_y;
                            int final_x = x + start_x;
                            int sum = (final_y * 256) + final_x;
                            int val = BMP_as_val[sum];

                            // this is dumb, make it better - TODO ??
                            // or don't, I don't care.
                            for (int i = 0; i < 12; i++) // skipping the universal color
                            {
                                if(nes_array[i] == val)
                                {
                                    cnt_array[i] = cnt_array[i] + 1;
                                    break;
                                }
                            }
                        }
                    }
                    // ok, we counted all the colors in the 16x16 block
                    
                    // if a color exists, add all its friends to the 12x12 array
                    // we are looking for associations within a 16x16 block
                    for(int i = 0; i < 12; i++) // check each color count
                    {
                        if(cnt_array[i] != 0)
                        {
                            int Times12 = i * 12;

                            // this loop needs to be refactored
                            for(int j = 0; j < 12; j++)
                            {
                                if (j == i) continue; // skip itself
                                Ar_12x12[Times12 + j] = Ar_12x12[Times12 + j] + cnt_array[j];
                            }
                        }
                    }
                }
            }

            // get best 3 colors for each color (skipping the universal color)

            int best_index = 0;
            int best_val = 0;
            int best1 = 0;
            int best2 = 0;
            int best3 = 0;
            int best4 = 0;
            int best5 = 0;
            int best6 = 0;

            for (int i = 0; i < 12; i++)
            {
                if (nes_array[i] == final_array[0]) continue;
                int Times3 = i * 3;
                int Times12 = i * 12;
                best_index = 0;
                if (i == 0) best_index = 1;
                best_val = 0;
                best1 = 0;
                best2 = 0;
                best3 = 0;
                
                for(int j = 0; j < 12; j++)
                {
                    if (j == i) continue; // skip itself
                    if (nes_array[j] == final_array[0]) continue;
                    int test = Ar_12x12[Times12 + j];
                    if(test > best_val)
                    {
                        best_index = j;
                        best_val = test;
                    }
                }
                if (best_val == 0) // we didn't find a single color ??
                {
                    // make a crappy 1 color palette
                    Temp_Palettes[Times3] = nes_array[i];
                    Temp_Palettes[Times3 + 1] = nes_array[11]; // TODO, this could be better
                    Temp_Palettes[Times3 + 2] = nes_array[10];
                    continue;
                }
                best1 = best_index;
                Ar_12x12[Times12 + best_index] = 0; // skip it next round

                best_index = 0;
                if (i == 0) best_index = 1;
                best_val = 0;
                for (int j = 0; j < 12; j++)
                {
                    if (j == i) continue; // skip itself
                    if (nes_array[j] == final_array[0]) continue;
                    int test = Ar_12x12[Times12 + j];
                    if (test > best_val)
                    {
                        best_index = j;
                        best_val = test;
                    }
                }
                if (best_val == 0) // only 1 color ??
                {
                    // make a crappy 2 color palette
                    Temp_Palettes[Times3] = nes_array[i];
                    Temp_Palettes[Times3 + 1] = nes_array[best1];
                    Temp_Palettes[Times3 + 2] = nes_array[11];
                    if(best1 == 11) // TODO, this could be better
                    {
                        Temp_Palettes[Times3 + 2] = nes_array[10];
                    }
                    continue; 
                }
                best2 = best_index;
                Ar_12x12[Times12 + best_index] = 0; // skip it next round

                // we have the 2 best colors, make 1 palettes of it

                Temp_Palettes[Times3] = nes_array[i]; // i;
                Temp_Palettes[Times3 + 1] = nes_array[best1]; // best1;
                Temp_Palettes[Times3 + 2] = nes_array[best2]; // best2;

                best_index = 0;
                if (i == 0) best_index = 1;
                best_val = 0;
                for (int j = 0; j < 12; j++)
                {
                    if (j == i) continue; // skip itself
                    if (nes_array[j] == final_array[0]) continue;
                    int test = Ar_12x12[Times12 + j];
                    if (test > best_val)
                    {
                        best_index = j;
                        best_val = test;
                    }
                }
                if (best_val == 0) continue; // only 2 color, skip last
                best3 = best_index;
                
                // make another palettes of it

                Temp_Palettes[Times3 + 36] = nes_array[i]; // i;
                Temp_Palettes[Times3 + 37] = nes_array[best1]; // best1;
                Temp_Palettes[Times3 + 38] = nes_array[best3]; // best3;
                
            }

            
            // Temp_Palettes[72] 12 * 2 * 3 colors
            // Final_Palettes[72]
            // Count_Palettes[72]
            // Ar_12x12[144]

            // sort each palette by value
            for (int i = 0; i < 72; i = i + 3)
            {
                int Times3 = i * 3;
                int temp = 0;
                if(Temp_Palettes[i] > Temp_Palettes[i + 1])
                { // swap
                    temp = Temp_Palettes[i];
                    Temp_Palettes[i] = Temp_Palettes[i + 1];
                    Temp_Palettes[i + 1] = temp;
                }
                if (Temp_Palettes[i] > Temp_Palettes[i + 2])
                { // swap
                    temp = Temp_Palettes[i];
                    Temp_Palettes[i] = Temp_Palettes[i + 2];
                    Temp_Palettes[i + 2] = temp;
                }
                if (Temp_Palettes[i + 1] > Temp_Palettes[i + 2])
                { // swap
                    temp = Temp_Palettes[i + 1];
                    Temp_Palettes[i + 1] = Temp_Palettes[i + 2];
                    Temp_Palettes[i + 2] = temp;
                }
            }

            // remove duplicate palettes, replace with -1 if empty
            for(int i = 0; i < 69; i = i + 3)
            {
                if (Temp_Palettes[i] == -1) continue;
                for (int j = i + 3; j < 72; j = j + 3)
                {
                    // just check downward, it would be redundant to check above
                    if (Temp_Palettes[j] == -1) continue;
                    if ((Temp_Palettes[i] == Temp_Palettes[j]) &&
                        (Temp_Palettes[i + 1] == Temp_Palettes[j + 1]) &&
                        (Temp_Palettes[i + 2] == Temp_Palettes[j + 2]))
                    {
                        // we found a duplicate palette, just put -1 in
                        Temp_Palettes[j] = -1;
                    }
                }
            }

            // count each palette, and copy to final

            count_palettes = 0;
            palette_index = 0;
            for (int i = 0; i < 72; i = i + 3)
            {
                if(Temp_Palettes[i] != -1)
                {
                    Final_Palettes[palette_index++] = Temp_Palettes[i];
                    Final_Palettes[palette_index++] = Temp_Palettes[i + 1];
                    Final_Palettes[palette_index++] = Temp_Palettes[i + 2];

                    //palette_index = palette_index + 3;
                    count_palettes++;
                }
            }



            if(count_palettes == 0)
            {
                // error? just fill
                final_array[1] = nes_array[11];
                final_array[2] = nes_array[10];
                final_array[3] = nes_array[9];
                final_array[4] = nes_array[8];
                final_array[5] = nes_array[7];
                final_array[6] = nes_array[6];
                final_array[7] = nes_array[5];
                final_array[8] = nes_array[4];
                final_array[9] = nes_array[3];
                final_array[10] = nes_array[2];
                final_array[11] = nes_array[1];
                final_array[12] = nes_array[0];

                Missing_Num();
                NES_to_Color();
                NES_to_Color2(); // extra array
                DRY_Palette(); // don't repeat yourself
                DRY_Palette2(); // extra array

                label3.Focus();
                return;
            }

            // then test each palette, counting good matches for each 16x16
            // each color found give +1, so for each 16x16 block possible 0-3 added
            // Final_Palettes[72]
            // Count_Palettes[24] 1 for each palette

            int pal_col1 = 0;
            int pal_col2 = 0;
            int pal_col3 = 0;

            for (int i = 0; i < count_palettes; i++)
            {
                int Times3 = i * 3;
                pal_col1 = Final_Palettes[Times3];
                if (pal_col1 != final_array[0]) // skip universal color
                {
                    // check every 16x16 block to see if color exists
                    int count = test16x16(num_blocks_x, num_blocks_y, pal_col1);

                    Count_Palettes[i] = Count_Palettes[i] + count;
                    
                }
                pal_col2 = Final_Palettes[Times3 + 1];
                if (pal_col2 != final_array[0]) // skip universal color
                {
                    if(pal_col2 != pal_col1)
                    {
                        // check every 16x16 block to see if color exists
                        int count = test16x16(num_blocks_x, num_blocks_y, pal_col2);

                        Count_Palettes[i] = Count_Palettes[i] + count;
                    }
                }
                pal_col3 = Final_Palettes[Times3 + 2];
                if (pal_col3 != final_array[0]) // skip universal color
                {
                    if ((pal_col3 != pal_col1) && (pal_col3 != pal_col2))
                    {
                        // check every 16x16 block to see if color exists
                        int count = test16x16(num_blocks_x, num_blocks_y, pal_col3);

                        Count_Palettes[i] = Count_Palettes[i] + count;
                    }
                }

            }


            // then get 4 (-6) best
            // and copy to final_array []
            best1 = 0;
            best2 = -1;
            best3 = -1;
            best4 = -1;
            best5 = -1;
            best6 = -1;
            best_val = 0;
            best_index = 0;

            for (int i = 0; i < 24; i++)
            {
                if(Count_Palettes[i] > best_val)
                {
                    best_val = Count_Palettes[i];
                    best_index = i;
                }
            }
            best1 = best_index * 3;
            Count_Palettes[best_index] = 0; // clear for next round
            best_val = 0;
            best_index = 0;
            for (int i = 0; i < 24; i++)
            {
                if (Count_Palettes[i] > best_val)
                {
                    best_val = Count_Palettes[i];
                    best_index = i;
                }
            }
            best2 = best_index * 3;
            Count_Palettes[best_index] = 0; // clear for next round
            if (best_val == 0) best2 = -1;
            best_val = 0;
            best_index = 0;
            for (int i = 0; i < 24; i++)
            {
                if (Count_Palettes[i] > best_val)
                {
                    best_val = Count_Palettes[i];
                    best_index = i;
                }
            }
            best3 = best_index * 3;
            Count_Palettes[best_index] = 0; // clear for next round
            if (best_val == 0) best3 = -1;
            best_val = 0;
            best_index = 0;
            for (int i = 0; i < 24; i++)
            {
                if (Count_Palettes[i] > best_val)
                {
                    best_val = Count_Palettes[i];
                    best_index = i;
                }
            }
            best4 = best_index * 3;
            Count_Palettes[best_index] = 0; // clear for next round
            if (best_val == 0) best4 = -1;
            best_val = 0;
            best_index = 0;
            for (int i = 0; i < 24; i++)
            {
                if (Count_Palettes[i] > best_val)
                {
                    best_val = Count_Palettes[i];
                    best_index = i;
                }
            }
            best5 = best_index * 3;
            Count_Palettes[best_index] = 0; // clear for next round
            if (best_val == 0) best5 = -1;
            best_val = 0;
            best_index = 0;
            for (int i = 0; i < 24; i++)
            {
                if (Count_Palettes[i] > best_val)
                {
                    best_val = Count_Palettes[i];
                    best_index = i;
                }
            }
            best6 = best_index * 3;
            Count_Palettes[best_index] = 0; // clear for next round
            if (best_val == 0) best6 = -1;

            // handle the -1 as black

            final_array[1] = Final_Palettes[best1];
            final_array[2] = Final_Palettes[best1 + 1];
            final_array[3] = Final_Palettes[best1 + 2];
            if(best2 >= 0)
            {
                final_array[4] = Final_Palettes[best2];
                final_array[5] = Final_Palettes[best2 + 1];
                final_array[6] = Final_Palettes[best2 + 2];
            }
            else
            {
                final_array[4] = 0;
                final_array[5] = 0;
                final_array[6] = 0;
            }
            if (best3 >= 0)
            {
                final_array[7] = Final_Palettes[best3];
                final_array[8] = Final_Palettes[best3 + 1];
                final_array[9] = Final_Palettes[best3 + 2];
            }
            else
            {
                final_array[7] = 0;
                final_array[8] = 0;
                final_array[9] = 0;
            }
            if (best4 >= 0)
            {
                final_array[10] = Final_Palettes[best4];
                final_array[11] = Final_Palettes[best4 + 1];
                final_array[12] = Final_Palettes[best4 + 2];
            }
            else
            {
                final_array[10] = 0;
                final_array[11] = 0;
                final_array[12] = 0;
            }


            // the extra 2 palettes
            if (best5 >= 0)
            {
                extra_array[0] = Final_Palettes[best5];
                extra_array[1] = Final_Palettes[best5 + 1];
                extra_array[2] = Final_Palettes[best5 + 2];
            }
            else
            {
                extra_array[0] = 0;
                extra_array[1] = 0;
                extra_array[2] = 0;
            }
            if (best6 >= 0)
            {
                extra_array[3] = Final_Palettes[best6];
                extra_array[4] = Final_Palettes[best6 + 1];
                extra_array[5] = Final_Palettes[best6 + 2];
            }
            else
            {
                extra_array[3] = 0;
                extra_array[4] = 0;
                extra_array[5] = 0;
            }
            
            //str = "Palettes ";
            //str += count_palettes.ToString();
            //label32.Text = str;


            // all palettes done


            Missing_Num();
            NES_to_Color();
            NES_to_Color2(); // extra array
            DRY_Palette(); // don't repeat yourself
            DRY_Palette2(); // extra array

            label3.Focus();

        }


        public int test16x16(int num_blocks_x, int num_blocks_y, int pal_col)
        {
            // see if a particular color exists in the current block
            int count = 0;
            
            for (int y2 = 0; y2 < num_blocks_y; y2++)
            {
                for (int x2 = 0; x2 < num_blocks_x; x2++)
                {
                    int start_y = 16 * y2;
                    int start_x = 16 * x2;

                    for (int y = 0; y < 16; y++)
                    {
                        for (int x = 0; x < 16; x++)
                        {
                            int final_y = y + start_y;
                            int final_x = x + start_x;
                            int sum = (final_y * 256) + final_x;
                            int val = BMP_as_val[sum];

                            if(pal_col == val)
                            {
                                count++;
                                goto Next;
                            }
                        }
                    }
                Next:
                    ;
                }
            }

            return count;
        }


        


        public void Missing_Num()
        { 
            // this function USED TO remove duplicate colors
            // or unused colors, and randomly replace them with colors

            // now it just replaces -1 with 0
            // which are unused palette slots, 0 = black

            for (int i = 0; i < 13; i++)
            {
                if (final_array[i] == -1)
                {
                    final_array[i] = 0;
                }
            }

            for (int i = 0; i < 6; i++)
            {
                if (extra_array[i] == -1)
                {
                    extra_array[i] = 0;
                }
            }
            
        }

        private void tooManyTilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("If you have too many tiles, you could\n" +
                "-reduce the size of the original\n" +
                "-make the backgroud a flat color\n" +
                "-turn off dithering\n" +
                "-blur or mosaic to the original");
            label3.Focus();
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            // brightness
            if (e.KeyChar == (char)Keys.Return)
            {
                set_brightness();
                label14.Text = "Tiles 0";

                e.Handled = true; // prevent ding on return press

                label3.Focus();
            }
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            // brightness
            set_brightness();
            label14.Text = "Tiles 0";
            label3.Focus();
        }

        public void set_brightness()
        {
            string str = textBox2.Text;
            int outvar = 0;
            if (int.TryParse(str, out outvar))
            {
                if (outvar > 100) outvar = 100;
                if (outvar < -100) outvar = -100;
                bright_adj = outvar;
                textBox2.Text = outvar.ToString();
            }
            else
            {
                // revert back to previous
                textBox2.Text = bright_adj.ToString();
            }
            Convert_Bright();
            
            // if view = revert, then let's see it

            if(has_loaded != 0)
            {
                Copy_2_Left();
                has_converted = 0;
            }
        }


        

        public void Convert_Bright()
        {
            // copies from original to bright bmp, but doesn't view it
            if (has_loaded == 0)
            {
                return;
            }
            Color tempcolor = Color.Black;
            int red, green, blue;
            // image_width, image_height
            for (int y = 0; y < image_height; y++)
            {
                for (int x = 0; x < image_width; x++)
                {
                    tempcolor = orig_bmp.GetPixel(x, y);
                    red = tempcolor.R + bright_adj;
                    red = Clamp255(red);
                    green = tempcolor.G + bright_adj;
                    green = Clamp255(green);
                    blue = tempcolor.B + bright_adj;
                    blue = Clamp255(blue);
                    bright_bmp.SetPixel(x, y, Color.FromArgb(red, green, blue));
                }
            }
        }


        public void Copy_2_Left()
        {
            // I'm not sure why I used the left_bmp 
            // but I don't want to try to redo it
            Color tempcolor = Color.Black;
            
            for (int y = 0; y < image_height; y++)
            {
                for (int x = 0; x < image_width; x++)
                {
                    tempcolor = bright_bmp.GetPixel(x, y);
                    
                    left_bmp.SetPixel(x, y, tempcolor);
                }
            }

            // and set the main picturebox
            pictureBox1.Image = left_bmp;
        }

        private void pictureBox19_Click(object sender, EventArgs e)
        {
            // alt 1, box #1
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val_extra[0];
                sel_color = col_array_extra[0];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label2.Text;
            }
            else // left click
            {
                col_val_extra[0] = sel_color_val;
                col_array_extra[0] = sel_color;
                pictureBox19.BackColor = sel_color;
                label2.Text = label5.Text;
            }

            label3.Focus();
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            // alt 1, box #2
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val_extra[1];
                sel_color = col_array_extra[1];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label12.Text;
            }
            else // left click
            {
                col_val_extra[1] = sel_color_val;
                col_array_extra[1] = sel_color;
                pictureBox18.BackColor = sel_color;
                label12.Text = label5.Text;
            }

            label3.Focus();
        }

        private void pictureBox17_Click(object sender, EventArgs e)
        {
            // alt 1, box #3
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val_extra[2];
                sel_color = col_array_extra[2];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label27.Text;
            }
            else // left click
            {
                col_val_extra[2] = sel_color_val;
                col_array_extra[2] = sel_color;
                pictureBox17.BackColor = sel_color;
                label27.Text = label5.Text;
            }

            label3.Focus();
        }

        private void pictureBox22_Click(object sender, EventArgs e)
        {
            // alt 2, box #1
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val_extra[3];
                sel_color = col_array_extra[3];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label28.Text;
            }
            else // left click
            {
                col_val_extra[3] = sel_color_val;
                col_array_extra[3] = sel_color;
                pictureBox22.BackColor = sel_color;
                label28.Text = label5.Text;
            }

            label3.Focus();
        }

        private void pictureBox21_Click(object sender, EventArgs e)
        {
            // alt 2, box #2
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val_extra[4];
                sel_color = col_array_extra[4];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label29.Text;
            }
            else // left click
            {
                col_val_extra[4] = sel_color_val;
                col_array_extra[4] = sel_color;
                pictureBox21.BackColor = sel_color;
                label29.Text = label5.Text;
            }

            label3.Focus();
        }

        private void pictureBox23_Click(object sender, EventArgs e)
        { // override value
            //over_val
            //over_color
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = over_val;
                sel_color = over_color;
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label34.Text;
            }
            else // left click
            {
                over_val = sel_color_val;
                over_color = sel_color;
                pictureBox23.BackColor = sel_color;
                label34.Text = label5.Text;
            }

            label3.Focus();
        }

        private void checkBox3_Click(object sender, EventArgs e)
        {
            if(override_cb == false)
            {
                override_cb = true;
                checkBox3.Checked = true;
            }
            else
            {
                override_cb = false;
                checkBox3.Checked = false;
            }
        }

        private void checkBox4_Click(object sender, EventArgs e)
        {
            // turn on / off palette 1
            if(pal1_on == true)
            {
                pal1_on = false;
                checkBox4.Checked = false;
            }
            else
            {
                pal1_on = true;
                checkBox4.Checked = true;
            }
        }

        private void checkBox5_Click(object sender, EventArgs e)
        {
            // turn on / off palette 2
            if (pal2_on == true)
            {
                pal2_on = false;
                checkBox5.Checked = false;
            }
            else
            {
                pal2_on = true;
                checkBox5.Checked = true;
            }
        }

        private void checkBox6_Click(object sender, EventArgs e)
        {
            // turn on / off palette 1
            if (pal3_on == true)
            {
                pal3_on = false;
                checkBox6.Checked = false;
            }
            else
            {
                pal3_on = true;
                checkBox6.Checked = true;
            }
        }

        private void checkBox7_Click(object sender, EventArgs e)
        {
            // turn on / off palette 1
            if (pal4_on == true)
            {
                pal4_on = false;
                checkBox7.Checked = false;
            }
            else
            {
                pal4_on = true;
                checkBox7.Checked = true;
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox4.SelectedIndex == 1)
            {
                lt_click_mode = 1;
            }
            else
            {
                lt_click_mode = 0;
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // preset clear
            pictureBox3.BackColor = Color.Black; // selected
            sel_color = Color.Black;
            sel_color_val = 0;
            label5.Text = "$0f";

            pictureBox23.BackColor = Color.Black; // main override
            over_color = Color.Black;
            over_val = 0;
            label34.Text = "$0f";

            for(int i = 0; i < 13; i++)
            {
                col_val[i] = 0;
                col_array[i] = Color.Black;
            }
            for (int i = 0; i < 6; i++)
            {
                col_val_extra[i] = 0;
                col_array_extra[i] = Color.Black;
            }

            DRY_Palette(); // col_array[]
            DRY_Palette2(); // col_array_extra[]
            label3.Focus();
        }

        private void graysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // preset grays
            
            for (int i = 0; i < 13; i++)
            {
                col_val[i] = 0;
                col_array[i] = Color.Black;
            }
            for (int i = 0; i < 6; i++)
            {
                col_val_extra[i] = 0;
                col_array_extra[i] = Color.Black;
            }
            col_val[1] = 12;
            col_val[2] = 13;
            col_val[3] = 26;
            col_val[4] = 13;
            col_val[5] = 26;
            col_val[6] = 39;
            col_array[1] = Conv_Val_2_Color(col_val[1]);
            col_array[2] = Conv_Val_2_Color(col_val[2]);
            col_array[3] = Conv_Val_2_Color(col_val[3]);
            col_array[4] = Conv_Val_2_Color(col_val[4]);
            col_array[5] = Conv_Val_2_Color(col_val[5]);
            col_array[6] = Conv_Val_2_Color(col_val[6]);

            DRY_Palette(); // col_array[]
            DRY_Palette2(); // col_array_extra[]
            label3.Focus();
        }


        public Color Conv_Val_2_Color(int val)
        {
            int index = val * 3;
            int red = NES_PALETTE[index];
            index++;
            int green = NES_PALETTE[index];
            index++;
            int blue = NES_PALETTE[index];
            return Color.FromArgb(red, green, blue);
        }

        private void blueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // preset blues
            
            for (int i = 0; i < 13; i++)
            {
                col_val[i] = 0;
                col_array[i] = Color.Black;
            }
            for (int i = 0; i < 6; i++)
            {
                col_val_extra[i] = 0;
                col_array_extra[i] = Color.Black;
            }
            col_val[1] = 1;
            col_val[2] = 14;
            col_val[3] = 27;
            col_val[4] = 14;
            col_val[5] = 27;
            col_val[6] = 40;
            col_array[1] = Conv_Val_2_Color(col_val[1]);
            col_array[2] = Conv_Val_2_Color(col_val[2]);
            col_array[3] = Conv_Val_2_Color(col_val[3]);
            col_array[4] = Conv_Val_2_Color(col_val[4]);
            col_array[5] = Conv_Val_2_Color(col_val[5]);
            col_array[6] = Conv_Val_2_Color(col_val[6]);

            DRY_Palette(); // col_array[]
            DRY_Palette2(); // col_array_extra[]
            label3.Focus();
        }

        private void redToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // preset reds
            
            for (int i = 0; i < 13; i++)
            {
                col_val[i] = 0;
                col_array[i] = Color.Black;
            }
            for (int i = 0; i < 6; i++)
            {
                col_val_extra[i] = 0;
                col_array_extra[i] = Color.Black;
            }
            col_val[1] = 6;
            col_val[2] = 19;
            col_val[3] = 32;
            col_val[4] = 19;
            col_val[5] = 32;
            col_val[6] = 45;
            col_array[1] = Conv_Val_2_Color(col_val[1]);
            col_array[2] = Conv_Val_2_Color(col_val[2]);
            col_array[3] = Conv_Val_2_Color(col_val[3]);
            col_array[4] = Conv_Val_2_Color(col_val[4]);
            col_array[5] = Conv_Val_2_Color(col_val[5]);
            col_array[6] = Conv_Val_2_Color(col_val[6]);

            DRY_Palette(); // col_array[]
            DRY_Palette2(); // col_array_extra[]
            label3.Focus();
        }

        private void greenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // preset greens
            
            for (int i = 0; i < 13; i++)
            {
                col_val[i] = 0;
                col_array[i] = Color.Black;
            }
            for (int i = 0; i < 6; i++)
            {
                col_val_extra[i] = 0;
                col_array_extra[i] = Color.Black;
            }
            col_val[1] = 9;
            col_val[2] = 22;
            col_val[3] = 35;
            col_val[4] = 22;
            col_val[5] = 35;
            col_val[6] = 48;
            col_array[1] = Conv_Val_2_Color(col_val[1]);
            col_array[2] = Conv_Val_2_Color(col_val[2]);
            col_array[3] = Conv_Val_2_Color(col_val[3]);
            col_array[4] = Conv_Val_2_Color(col_val[4]);
            col_array[5] = Conv_Val_2_Color(col_val[5]);
            col_array[6] = Conv_Val_2_Color(col_val[6]);

            DRY_Palette(); // col_array[]
            DRY_Palette2(); // col_array_extra[]
            label3.Focus();
        }

        private void sMBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // preset SMB

            for (int i = 0; i < 13; i++)
            {
                col_val[i] = 0;
                col_array[i] = Color.Black;
            }
            for (int i = 0; i < 6; i++)
            {
                col_val_extra[i] = 0;
                col_array_extra[i] = Color.Black;
            }
            col_val[0] = 28;
            col_val[1] = 0;
            col_val[2] = 23;
            col_val[3] = 35;
            col_val[4] = 0;
            col_val[5] = 20;
            col_val[6] = 45;
            col_val[7] = 0;
            col_val[8] = 27;
            col_val[9] = 39;
            col_val[10] = 0;
            col_val[11] = 20;
            col_val[12] = 33;
            col_array[0] = Conv_Val_2_Color(col_val[0]);
            col_array[1] = Conv_Val_2_Color(col_val[1]);
            col_array[2] = Conv_Val_2_Color(col_val[2]);
            col_array[3] = Conv_Val_2_Color(col_val[3]);
            col_array[4] = Conv_Val_2_Color(col_val[4]);
            col_array[5] = Conv_Val_2_Color(col_val[5]);
            col_array[6] = Conv_Val_2_Color(col_val[6]);
            col_array[7] = Conv_Val_2_Color(col_val[7]);
            col_array[8] = Conv_Val_2_Color(col_val[8]);
            col_array[9] = Conv_Val_2_Color(col_val[9]);
            col_array[10] = Conv_Val_2_Color(col_val[10]);
            col_array[11] = Conv_Val_2_Color(col_val[11]);
            col_array[12] = Conv_Val_2_Color(col_val[12]);

            DRY_Palette(); // col_array[]
            DRY_Palette2(); // col_array_extra[]
            label3.Focus();
        }

        private void pictureBox20_Click(object sender, EventArgs e)
        {
            // alt 2, box #3
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val_extra[5];
                sel_color = col_array_extra[5];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label30.Text;
            }
            else // left click
            {
                col_val_extra[5] = sel_color_val;
                col_array_extra[5] = sel_color;
                pictureBox20.BackColor = sel_color;
                label30.Text = label5.Text;
            }

            label3.Focus();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            label3.Focus();
        }

        public void NES_to_Color()
        {
            int temp_c = 0;
            //39 (0x27) is white, make it 99
            //to make sure it is the far right of a palette
            for(int i = 0; i < 13; i++)
            {
                if (final_array[i] == 39) final_array[i] = 99;
            }

            // flip the colors in numerical order
            // 1 2 3
            if (final_array[2] < final_array[1])
            {
                temp_c = final_array[1];
                final_array[1] = final_array[2];
                final_array[2] = temp_c;
            }
            if (final_array[3] < final_array[2])
            {
                temp_c = final_array[2];
                final_array[2] = final_array[3];
                final_array[3] = temp_c;
            }
            if (final_array[2] < final_array[1])
            {
                temp_c = final_array[1];
                final_array[1] = final_array[2];
                final_array[2] = temp_c;
            }
            // 4 5 6
            if (final_array[5] < final_array[4])
            {
                temp_c = final_array[4];
                final_array[4] = final_array[5];
                final_array[5] = temp_c;
            }
            if (final_array[6] < final_array[5])
            {
                temp_c = final_array[5];
                final_array[5] = final_array[6];
                final_array[6] = temp_c;
            }
            if (final_array[5] < final_array[4])
            {
                temp_c = final_array[4];
                final_array[4] = final_array[5];
                final_array[5] = temp_c;
            }
            // 7 8 9
            if (final_array[8] < final_array[7])
            {
                temp_c = final_array[7];
                final_array[7] = final_array[8];
                final_array[8] = temp_c;
            }
            if (final_array[9] < final_array[8])
            {
                temp_c = final_array[8];
                final_array[8] = final_array[9];
                final_array[9] = temp_c;
            }
            if (final_array[8] < final_array[7])
            {
                temp_c = final_array[7];
                final_array[7] = final_array[8];
                final_array[8] = temp_c;
            }
            // 10 11 12
            if (final_array[11] < final_array[10])
            {
                temp_c = final_array[10];
                final_array[10] = final_array[11];
                final_array[11] = temp_c;
            }
            if (final_array[12] < final_array[11])
            {
                temp_c = final_array[11];
                final_array[11] = final_array[12];
                final_array[12] = temp_c;
            }
            if (final_array[11] < final_array[10])
            {
                temp_c = final_array[10];
                final_array[10] = final_array[11];
                final_array[11] = temp_c;
            }

            //put white back the way it's supposed to be, make it 39
            for (int i = 0; i < 13; i++)
            {
                if (final_array[i] == 99) final_array[i] = 39;

                col_val[i] = final_array[i];
            }


            // convert to a color array
            for (int i = 0; i < 13; i++)
            {
                int times3 = final_array[i] * 3;
                r_val = NES_PALETTE[times3];
                g_val = NES_PALETTE[times3 + 1];
                b_val = NES_PALETTE[times3 + 2];
                col_array[i] = Color.FromArgb(r_val, g_val, b_val);
            }

            // count unique colors

            int count_colors = 1;
            
            for (int i = 1; i < 13; i++)
            {
                bool unique = count_sub(i);
                if (unique == true) count_colors++;
            }

            label24.Text = count_colors.ToString(); 
        }


        public void NES_to_Color2()
        {
            int temp_c = 0;
            //39 (0x27) is white, make it 99
            //to make sure it is the far right of a palette
            for (int i = 0; i < 6; i++)
            {
                if (extra_array[i] == 39) extra_array[i] = 99;
            }

            // flip the colors in numerical order
            // 0 1 2
            if (extra_array[1] < extra_array[0])
            {
                temp_c = extra_array[0];
                extra_array[0] = extra_array[1];
                extra_array[1] = temp_c;
            }
            if (extra_array[2] < extra_array[1])
            {
                temp_c = extra_array[1];
                extra_array[1] = extra_array[2];
                extra_array[2] = temp_c;
            }
            if (extra_array[1] < extra_array[0])
            {
                temp_c = extra_array[0];
                extra_array[0] = extra_array[1];
                extra_array[1] = temp_c;
            }
            // 3 4 5
            if (extra_array[4] < extra_array[3])
            {
                temp_c = extra_array[3];
                extra_array[3] = extra_array[4];
                extra_array[4] = temp_c;
            }
            if (extra_array[5] < extra_array[4])
            {
                temp_c = extra_array[4];
                extra_array[4] = extra_array[5];
                extra_array[5] = temp_c;
            }
            if (extra_array[4] < extra_array[3])
            {
                temp_c = extra_array[3];
                extra_array[3] = extra_array[4];
                extra_array[4] = temp_c;
            }
            

            //put white back the way it's supposed to be, make it 39
            for (int i = 0; i < 6; i++)
            {
                if (extra_array[i] == 99) extra_array[i] = 39;

                col_val_extra[i] = extra_array[i];
            }


            // convert to a color array
            for (int i = 0; i < 6; i++)
            {
                int times3 = extra_array[i] * 3;
                r_val = NES_PALETTE[times3];
                g_val = NES_PALETTE[times3 + 1];
                b_val = NES_PALETTE[times3 + 2];
                col_array_extra[i] = Color.FromArgb(r_val, g_val, b_val);
            }

            
        }



        public bool count_sub(int i)
        {
            for(int j = 0; j < i; j++)
            {
                if(final_array[i] == final_array[j])
                {
                    return false;
                }
            }
            return true;
        }

        

        public void do_dither(int red, int green, int blue, int xx, int yy)
        {
            // floyd steinburg dithering method
            // push the error to
            // -    0    7/16
            // 3/16 5/16 1/16

            double red_db = (double)red;
            double green_db = (double)green;
            double blue_db = (double)blue;

            Color tempcolor = Color.Black;

            //dither_db is a global, already set

            // right side = 7/16
            if (xx < image_width - 1)
            {
                tempcolor = scratch_bmp.GetPixel(xx + 1, yy);
                red = (int)((red_db * dither_db * 0.4375) + tempcolor.R);
                red = Clamp255(red);
                green = (int)((green_db * dither_db * 0.4375) + tempcolor.G);
                green = Clamp255(green);
                blue = (int)((blue_db * dither_db * 0.4375) + tempcolor.B);
                blue = Clamp255(blue);
                tempcolor = Color.FromArgb(red, green, blue);
                scratch_bmp.SetPixel(xx + 1, yy, tempcolor);
            }

            // below
            if (yy < image_height - 1)
            {
                if (xx > 0)
                { // below left = 3/16
                    tempcolor = scratch_bmp.GetPixel(xx - 1, yy + 1);
                    red = (int)((red_db * dither_db * 0.1875) + tempcolor.R);
                    red = Clamp255(red);
                    green = (int)((green_db * dither_db * 0.1875) + tempcolor.G);
                    green = Clamp255(green);
                    blue = (int)((blue_db * dither_db * 0.1875) + tempcolor.B);
                    blue = Clamp255(blue);
                    tempcolor = Color.FromArgb(red, green, blue);
                    scratch_bmp.SetPixel(xx - 1, yy + 1, tempcolor);
                }

                // below = 5/16
                tempcolor = scratch_bmp.GetPixel(xx, yy + 1);
                red = (int)((red_db * dither_db * 0.3125) + tempcolor.R);
                red = Clamp255(red);
                green = (int)((green_db * dither_db * 0.3125) + tempcolor.G);
                green = Clamp255(green);
                blue = (int)((blue_db * dither_db * 0.3125) + tempcolor.B);
                blue = Clamp255(blue);
                tempcolor = Color.FromArgb(red, green, blue);
                scratch_bmp.SetPixel(xx, yy + 1, tempcolor);

                if (xx < image_width - 1)
                { // below right = 1/16
                    tempcolor = scratch_bmp.GetPixel(xx + 1, yy + 1);
                    red = (int)((red_db * dither_db * 0.0625) + tempcolor.R);
                    red = Clamp255(red);
                    green = (int)((green_db * dither_db * 0.0625) + tempcolor.G);
                    green = Clamp255(green);
                    blue = (int)((blue_db * dither_db * 0.0625) + tempcolor.B);
                    blue = Clamp255(blue);
                    tempcolor = Color.FromArgb(red, green, blue);
                    scratch_bmp.SetPixel(xx + 1, yy + 1, tempcolor);
                }
            }
        }

        

        private void pictureBox15_Click(object sender, EventArgs e)
        { // pal #3, color #2
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[11];
                sel_color = col_array[11];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label22.Text;
            }
            else // left click
            {
                col_val[11] = sel_color_val;
                col_array[11] = sel_color;
                pictureBox15.BackColor = sel_color;
                label22.Text = label5.Text;
            }
            
            label3.Focus();
        }

        private void pictureBox16_Click(object sender, EventArgs e)
        { // pal #3, color #3
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == MouseButtons.Right)
            {
                sel_color_val = col_val[12];
                sel_color = col_array[12];
                pictureBox3.BackColor = sel_color; // select color box
                label5.Text = label23.Text;
            }
            else // left click
            {
                col_val[12] = sel_color_val;
                col_array[12] = sel_color;
                pictureBox16.BackColor = sel_color;
                label23.Text = label5.Text;
            }
            
            label3.Focus();
        }

        public string GetNesVal(int index)
        {
            if (index < 0) return "error 1";
            if (index > 51) return "error 2";
            if (index == 0) return "$0f";
            if (index == 1) return "$01";
            if (index == 2) return "$02";
            if (index == 3) return "$03";
            if (index == 4) return "$04";
            if (index == 5) return "$05";
            if (index == 6) return "$06";
            if (index == 7) return "$07";
            if (index == 8) return "$08";
            if (index == 9) return "$09";
            if (index == 10) return "$0a";
            if (index == 11) return "$0b";
            if (index == 12) return "$0c";
            if (index == 13) return "$00";
            if (index == 14) return "$11";
            if (index == 15) return "$12";
            if (index == 16) return "$13";
            if (index == 17) return "$14";
            if (index == 18) return "$15";
            if (index == 19) return "$16";
            if (index == 20) return "$17";
            if (index == 21) return "$18";
            if (index == 22) return "$19";
            if (index == 23) return "$1a";
            if (index == 24) return "$1b";
            if (index == 25) return "$1c";
            if (index == 26) return "$10";
            if (index == 27) return "$21";
            if (index == 28) return "$22";
            if (index == 29) return "$23";
            if (index == 30) return "$24";
            if (index == 31) return "$25";
            if (index == 32) return "$26";
            if (index == 33) return "$27";
            if (index == 34) return "$28";
            if (index == 35) return "$29";
            if (index == 36) return "$2a";
            if (index == 37) return "$2b";
            if (index == 38) return "$2c";
            if (index == 39) return "$30";
            if (index == 40) return "$31";
            if (index == 41) return "$32";
            if (index == 42) return "$33";
            if (index == 43) return "$34";
            if (index == 44) return "$35";
            if (index == 45) return "$36";
            if (index == 46) return "$37";
            if (index == 47) return "$38";
            if (index == 48) return "$39";
            if (index == 49) return "$3a";
            if (index == 50) return "$3b";
            if (index == 51) return "$3c";
            return "error 3";
        }

        private void pasteFromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        { // load image from clipboard
            paste_clipboard();
            label3.Focus();
        }


        public void paste_clipboard()
        {
            IDataObject myClip = Clipboard.GetDataObject();

            if (myClip.GetDataPresent(DataFormats.Bitmap))
            {
                Bitmap temp_bmp = new Bitmap(256, 240);
                temp_bmp = myClip.GetData(DataFormats.Bitmap) as Bitmap;

                // NOTE this is identical to the Import Image code
                // any changes need to happen on both sets of code
                // lots of redundant code...
                // todo, make these 2 functions into 1 common function

                has_loaded = 1;
                has_converted = 0;

                // clear it
                for (int y = 0; y < MAX_HEIGHT; y++)
                {
                    for (int x = 0; x < MAX_WIDTH; x++)
                    {
                        orig_bmp.SetPixel(x, y, Color.Black);
                    }
                }


                float ratio1 = 1.0F;
                float ratio2 = 1.0F;
                int resize_width = MAX_WIDTH, resize_height = MAX_HEIGHT;
                int need_resize = 0;

                if ((temp_bmp.Width < 16) || (temp_bmp.Height < 16))
                {
                    MessageBox.Show("Error. Needs to be at least 16x16");

                    temp_bmp.Dispose();
                    label3.Focus();
                    return;
                }

                if (temp_bmp.Width > MAX_WIDTH)
                {
                    image_width = MAX_WIDTH;
                    ratio1 = temp_bmp.Width / (float)MAX_WIDTH;
                    need_resize = 1;
                }
                else
                {
                    image_width = temp_bmp.Width;
                }

                if (temp_bmp.Height > MAX_HEIGHT)
                {
                    image_height = MAX_HEIGHT;
                    ratio2 = temp_bmp.Height / (float)MAX_HEIGHT;
                    need_resize = 1;
                }
                else
                {
                    image_height = temp_bmp.Height;
                }

                // copy the bitmap
                if ((checkBox1.Checked == true) && (need_resize == 1))
                {
                    // which is bigger? divide by that
                    if (ratio1 > ratio2)
                    {
                        resize_width = (int)Math.Round(temp_bmp.Width / ratio1);
                        resize_height = (int)Math.Round(temp_bmp.Height / ratio1);
                    }
                    else
                    {
                        resize_width = (int)Math.Round(temp_bmp.Width / ratio2);
                        resize_height = (int)Math.Round(temp_bmp.Height / ratio2);
                    }

                    // resize to fit
                    using (Graphics g2 = Graphics.FromImage(orig_bmp))
                    {
                        g2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g2.DrawImage(temp_bmp, 0, 0, resize_width, resize_height);
                    }

                    image_width = resize_width;
                    image_height = resize_height;
                }
                else
                {
                    // copy the bitmap, crop but don't resize
                    Rectangle copyRect = new Rectangle(0, 0, image_width, image_height);
                    using (Graphics g2 = Graphics.FromImage(orig_bmp))
                    {
                        g2.DrawImage(temp_bmp, copyRect, copyRect, GraphicsUnit.Pixel);
                    }


                }

                Color temp_color = Color.Black;

                // copy pixel by pixel
                for (int xx = 0; xx < MAX_WIDTH; xx++)
                {
                    for (int yy = 0; yy < MAX_HEIGHT; yy++)
                    {
                        if ((xx < image_width) && (yy < image_height))
                        {
                            temp_color = orig_bmp.GetPixel(xx, yy);
                        }
                        else
                        {
                            temp_color = Color.Gray;
                        }
                        left_bmp.SetPixel(xx, yy, temp_color);
                    }
                }


                // show in picture box
                //pictureBox1.Image = left_bmp;
                //pictureBox1.Refresh();
                bright_adj = 0;
                Convert_Bright();
                Copy_2_Left();

                // show the width and height
                label11.Text = image_width.ToString();
                label24.Text = image_height.ToString();
                textBox2.Text = "0";
                
                //label6.Text = "Loaded";
                label14.Text = "Tiles 0";
                temp_bmp.Dispose();
            }
            else
            {
                MessageBox.Show("Clipboard is not in bitmap format.");
            }
        }


        private void loadImageToolStripMenuItem_Click(object sender, EventArgs e)
        { // load image, import an image 256x240 max
            // open dialogue, load image file

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open Image";
                dlg.Filter = "Image Files .png .jpg .bmp .gif)|*.png;*.jpg;*.bmp;*.gif|" + "All Files (*.*)|*.*";


                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    has_loaded = 1;
                    has_converted = 0;

                    // clear it
                    for (int y = 0; y < MAX_HEIGHT; y++)
                    {
                        for (int x = 0; x < MAX_WIDTH; x++)
                        {
                            orig_bmp.SetPixel(x, y, Color.Black);
                        }
                    }

                    Bitmap temp_bmp = new Bitmap(dlg.FileName);

                    float ratio1 = 1.0F;
                    float ratio2 = 1.0F;
                    int resize_width = MAX_WIDTH, resize_height = MAX_HEIGHT;
                    int need_resize = 0;

                    if((temp_bmp.Width < 16) || (temp_bmp.Height < 16))
                    {
                        MessageBox.Show("Error. Needs to be at least 16x16");

                        temp_bmp.Dispose();
                        label3.Focus();
                        return;
                    }

                    if (temp_bmp.Width > MAX_WIDTH)
                    {
                        image_width = MAX_WIDTH;
                        ratio1 = temp_bmp.Width / (float)MAX_WIDTH;
                        need_resize = 1;
                    }
                    else
                    {
                        image_width = temp_bmp.Width;
                    }

                    if (temp_bmp.Height > MAX_HEIGHT)
                    {
                        image_height = MAX_HEIGHT;
                        ratio2 = temp_bmp.Height / (float)MAX_HEIGHT;
                        need_resize = 1;
                    }
                    else
                    {
                        image_height = temp_bmp.Height;
                    }

                    if ((checkBox1.Checked == true) && (need_resize == 1))
                    {
                        // which is bigger? divide by that
                        if (ratio1 > ratio2)
                        {
                            resize_width = (int)Math.Round(temp_bmp.Width / ratio1);
                            resize_height = (int)Math.Round(temp_bmp.Height / ratio1);
                        }
                        else
                        {
                            resize_width = (int)Math.Round(temp_bmp.Width / ratio2);
                            resize_height = (int)Math.Round(temp_bmp.Height / ratio2);
                        }

                        using (Graphics g2 = Graphics.FromImage(orig_bmp))
                        {
                            g2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g2.DrawImage(temp_bmp, 0, 0, resize_width, resize_height);
                        }

                        image_width = resize_width;
                        image_height = resize_height;
                    }
                    else
                    {
                        // copy the bitmap, crop but don't resize
                        Rectangle copyRect = new Rectangle(0, 0, image_width, image_height);
                        using (Graphics g2 = Graphics.FromImage(orig_bmp))
                        {
                            g2.DrawImage(temp_bmp, copyRect, copyRect, GraphicsUnit.Pixel);
                        }

                    }



                    Color temp_color = Color.Black;

                    // copy pixel by pixel
                    for (int xx = 0; xx < MAX_WIDTH; xx++)
                    {
                        for (int yy = 0; yy < MAX_HEIGHT; yy++)
                        {
                            if ((xx < image_width) && (yy < image_height))
                            {
                                temp_color = orig_bmp.GetPixel(xx, yy);
                            }
                            else
                            {
                                temp_color = Color.Gray;
                            }
                            left_bmp.SetPixel(xx, yy, temp_color);
                        }
                    }


                    // show in picture box
                    //pictureBox1.Image = left_bmp;
                    //pictureBox1.Refresh();
                    bright_adj = 0;
                    Convert_Bright();
                    Copy_2_Left();

                    // show the width and height
                    label11.Text = image_width.ToString();
                    label24.Text = image_height.ToString();

                    //label6.Text = "Loaded";
                    label14.Text = "Tiles 0";
                    textBox2.Text = "0";
                    
                    temp_bmp.Dispose(); // this fixed the lock up.
                }
                // it was locking up files, so...

                dlg.Dispose();
                GC.Collect();
            }
            label3.Focus();
        }


    }
}
