using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Station366.Common
{
    public class TwoStageConverter : IValueConverter
    {
        public IValueConverter First { get; set; }
        public IValueConverter Second { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            object convertedValue = First.Convert(value, targetType, parameter, language);
            return Second.Convert(convertedValue, targetType, parameter, language);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
