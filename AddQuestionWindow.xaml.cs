using System.Windows;
using Kuiz.Services;

namespace Kuiz
{
    public partial class AddQuestionWindow : Window
    {
        public int? EditingId { get; set; }
        private readonly QuestionService _questionService = new();
        
        public AddQuestionWindow()
        {
            InitializeComponent();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var text = TxtQ.Text?.Trim();
            var ans = TxtA.Text?.Trim();
            
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(ans))
            {
                MessageBox.Show("Question and answer required", "Add", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Application.Current.MainWindow is MainWindow mw)
            {
                try
                {
                    if (EditingId.HasValue)
                    {
                        // Update existing question via API
                        await _questionService.UpdateQuestionAsync(EditingId.Value, text, ans, "User");
                    }
                    else
                    {
                        // Create new question via API
                        await _questionService.AddQuestionAsync(text, ans, "User");
                    }

                    MessageBox.Show("Saved.", "Add", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                    mw.RefreshAndLoadQuestions();
                }
                catch (System.Exception ex)
                {
                    Logger.LogError(ex);
                    MessageBox.Show("API error: " + ex.Message, "Add", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

