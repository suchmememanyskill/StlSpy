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

        public NewCollectionView()
        {
            InitializeComponent();
            SetControls();
        }

        public NewCollectionView(Func<string, Task<string?>> onSubmit) : this()
        {
            _onSubmit = onSubmit;
        }

        public string MainText() => "Create";
        public string SubText() => "New collection";
        public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();

        [Command(nameof(Submit))]
        public async void SubmitCollection()
        {
            Submit.IsEnabled = false;

            string? s = CollectionName.Text;

            if (string.IsNullOrWhiteSpace(s))
            {
                Submit.IsEnabled = true;
                ErrorText.Content = "Collection name cannot be empty";
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