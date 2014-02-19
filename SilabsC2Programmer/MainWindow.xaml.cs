using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SilabsC2Programmer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //IntelHexBinaryConverter.ConvertIntelHexToBinary(@"C:\Users\yo02827\Downloads\bootloader.hm_trp.433.hex",
            //    @"C:\Users\yo02827\Downloads\bootloader.hm_trp.433.bin");

            //try
            //{
            //    IntelHexBinaryConverter.ConvertBinaryToIntelHex(@"C:\Users\yo02827\Downloads\bootloader.hm_trp.433.bin",
            //            @"C:\Users\yo02827\Downloads\bootloader.hm_trp.433-Test.hex", 16);
            //}
            //catch (Exception ex)
            //{
            //}
        }
    }
}
