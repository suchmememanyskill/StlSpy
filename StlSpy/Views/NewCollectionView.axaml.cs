using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;

namespace StlSpy.Views
{
    public partial class NewCollectionView : UserControlExt<NewCollectionView>, IMainView
    {
        private Func<string, Task<string?>>? _onSubmit;
        private string _mainText = "Create";
        private string _subText = "New collection";

        [Binding(nameof(CollectionName), "Watermark")]
        public string WatermarkText { get; } = "Enter Collection Name here";

        [Binding(nameof(Submit), "Content")] 
        public string SubmitButtonText { get; } = "Add Collection";
        
        public NewCollectionView()
        {
            InitializeComponent();
            SetControls();
        }

        public NewCollectionView(Func<string, Task<string?>> onSubmit) : this()
        {
            _onSubmit = onSubmit;
        }

        public NewCollectionView(Func<string, Task<string?>> onSubmit, string mainText, string subText,
            string watermarkText, string submitButtonText) : this(onSubmit)
        {
            _mainText = mainText;
            _subText = subText;
            WatermarkText = watermarkText;
            SubmitButtonText = submitButtonText;
            UpdateView();
        }

        public string MainText() => _mainText;
        public string SubText() => _subText;
        public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();

        [Command(nameof(Submit))]
        public async void SubmitCollection()
        {
            Submit.IsEnabled = false;

            string? s = CollectionName.Text;

            if (string.IsNullOrWhiteSpace(s))
            {
                Submit.IsEnabled = true;
                ErrorText.Content = "Input cannot be empty";
                return;
            }
            
            Task<string?>? result = _onSubmit?.Invoke(s);

            if (result != null)
            {
                string? response = await result;
                if (response != null)
                {
                    Submit.IsEnabled = true;
                    ErrorText.Content = response;
                }
            }
        }
    }
}