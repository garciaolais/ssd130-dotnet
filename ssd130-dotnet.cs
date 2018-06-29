using System;
using System.Collections.Generic;
using System.Linq;
using i2cdotnet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ssd130
{
    public class SSD130: I2c, IDisposable
    {
        const Int16 SSD1306_I2C_ADDRESS = 0x3C;
        const Int16 SSD1306_SETCONTRAST = 0x81;
        const Int16 SSD1306_DISPLAYALLON_RESUME = 0xA4;
        const Int16 SSD1306_DISPLAYALLON = 0xA5;
        const Int16 SSD1306_NORMALDISPLAY = 0xA6;
        const Int16 SSD1306_INVERTDISPLAY = 0xA7;
        const Int16 SSD1306_DISPLAYOFF = 0xAE;
        const Int16 SSD1306_DISPLAYON = 0xAF;
        const Int16 SSD1306_SETDISPLAYOFFSET = 0xD3;
        const Int16 SSD1306_SETCOMPINS = 0xDA;
        const Int16 SSD1306_SETVCOMDETECT = 0xDB;
        const Int16 SSD1306_SETDISPLAYCLOCKDIV = 0xD5;
        const Int16 SSD1306_SETPRECHARGE = 0xD9;
        const Int16 SSD1306_SETMULTIPLEX = 0xA8;
        const Int16 SSD1306_SETLOWCOLUMN = 0x00;
        const Int16 SSD1306_SETHIGHCOLUMN = 0x10;
        const Int16 SSD1306_SETSTARTLINE = 0x40;
        const Int16 SSD1306_MEMORYMODE = 0x20;
        const Int16 SSD1306_COLUMNADDR = 0x21;
        const Int16 SSD1306_PAGEADDR = 0x22;
        const Int16 SSD1306_COMSCANINC = 0xC0;
        const Int16 SSD1306_COMSCANDEC = 0xC8;
        const Int16 SSD1306_SEGREMAP = 0xA0;
        const Int16 SSD1306_CHARGEPUMP = 0x8D;
        const Int16 SSD1306_EXTERNALVCC = 0x1;
        const Int16 SSD1306_SWITCHCAPVCC = 0x2;

        // Scrolling constants
        const Int16 SSD1306_ACTIVATE_SCROLL = 0x2F;
        const Int16 SSD1306_DEACTIVATE_SCROLL = 0x2E;
        const Int16 SSD1306_SET_VERTICAL_SCROLL_AREA = 0xA3;
        const Int16 SSD1306_RIGHT_HORIZONTAL_SCROLL = 0x26;
        const Int16 SSD1306_LEFT_HORIZONTAL_SCROLL = 0x27;
        const Int16 SSD1306_VERTICAL_AND_RIGHT_HORIZONTAL_SCROLL = 0x29;
        const Int16 SSD1306_VERTICAL_AND_LEFT_HORIZONTAL_SCROLL = 0x2A;

        Int16 _vccstate = 0;

        //
        int fd = 0;
        const int addr = SSD1306_I2C_ADDRESS;
        Int16 width = 128;
        Int16 height = 32;
        Int16 _pages = 4;
        List<Int16> _buffer;
        Int16 contrast = 0;
        
       
        public SSD130()
        {         
            _buffer = new List<short>();
            fd = Open(String.Format("/dev/i2c-{0}", 1), 1);
            
        }  

        public void Begin(Int16 vccstate = SSD1306_SWITCHCAPVCC)
        {         
            this._vccstate = SSD1306_SWITCHCAPVCC;
            Reset();
            _initialize();
            Command(SSD1306_DISPLAYON);
        }
        
        public void Close()
        {
            if (!(fd <= 0))
                Close(fd);
        }

        void _initialize()
        {         
            Command(SSD1306_DISPLAYOFF);
            Command(SSD1306_SETDISPLAYCLOCKDIV);
            Command(0x80);
            Command(SSD1306_SETMULTIPLEX);
            Command(0x1F);
            Command(SSD1306_SETDISPLAYOFFSET);
            Command(0x0);
            Command(SSD1306_SETSTARTLINE);
            Command(SSD1306_CHARGEPUMP);

            if (_vccstate == SSD1306_EXTERNALVCC)
            {
                Command(0x10);
            }
            else
            {
                Command(0x14);
            }
            Command(SSD1306_MEMORYMODE);
            Command(0x00);
            Command(SSD1306_SEGREMAP | 0x1);
            Command(SSD1306_COMSCANDEC);
            Command(SSD1306_SETCOMPINS);
            Command(0x02);
            Command(SSD1306_SETCONTRAST);
            Command(0x8F);
            Command(SSD1306_SETPRECHARGE);
            if (_vccstate == SSD1306_EXTERNALVCC)
                Command(0x22);
            else
                Command(0xF1);            
            Command(SSD1306_SETVCOMDETECT);
            Command(0x40);
            Command(SSD1306_DISPLAYALLON_RESUME);
            Command(SSD1306_NORMALDISPLAY);
        }

        public void Command(Int16 command)
        {           
            Int16 control = 0x00;
            RegWriteByte(fd, addr, control, command);
        }

        public void Data(Int16 command)
        {         
            Int16 control = 0x40;
            RegWriteByte(fd, addr, control, command);
        }

       
        public void Reset() { }
        public void Display()
        {            
            Command(SSD1306_COLUMNADDR);
            Command(0);
            Command((Int16)(width - 1));
            Command(SSD1306_PAGEADDR);
            Command(0);
            Command((Int16)(_pages - 1));
            
            foreach (var i in Enumerable.Range(0,_buffer.Count).Select(x=> x * 16)){
                if (i >= _buffer.Count) break;
                Int16 control = 0x40;
                var _bufer_range = _buffer.Skip(i).Take(16).ToArray();
                RegWriteBytes(fd, addr, control, _buffer.Skip(i).Take(16).ToArray());
                
            }            
        }

        public void Image(Image<Rgba32> image)
        {         
            var index = 0;
            for (int pages = 0; pages < _pages; pages++)
            {
                for (int x = 0; x < width; x++)
                {
                    var bits = 0;
                    for (int bit = 0; bit < 8; bit++)
                    {
                        bits = bits << 1;
                        bits |= image[x, pages * 8 + 7 - bit].R > 0 ? 1 : 0;
                        
                    }

                    _buffer[index]= (Int16)bits;
                    index += 1;
                }
            }            
        }

        public void Clear()
        {            
            _buffer = new List<short>(new Int16[512]);         
        }

        public void Contrast(Int16 contrast)
        {         
            if (contrast < 0 || contrast > 255)
            {
         
            }
            Command(SSD1306_SETCONTRAST);
            Command(contrast);
        }

        public void Dim(bool dim)
        {            
            contrast = 0;

            if (!dim)
            {
                if(_vccstate == SSD1306_EXTERNALVCC)
                {
                    contrast = 0x9F;
                }
                else
                {
                    contrast = 0xCF;
                }
            }
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Close(fd);
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
