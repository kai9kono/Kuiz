using System.Windows;
using Npgsql;
using Kuiz.Services;

namespace Kuiz
{
    public partial class AddQuestionWindow : Window
    {
        public int? EditingId { get; set; }
        
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
                    await using var c = new NpgsqlConnection(QuestionService.DefaultConnectionString);
                    await c.OpenAsync();
                    
                    if (EditingId.HasValue)
                    {
                        var cmd = new NpgsqlCommand("UPDATE questions SET text=@t, answer=@a WHERE id=@id", c);
                        cmd.Parameters.AddWithValue("@t", text);
                        cmd.Parameters.AddWithValue("@a", ans);
                        cmd.Parameters.AddWithValue("@id", EditingId.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        var cmd = new NpgsqlCommand("INSERT INTO questions (text, answer) VALUES (@t,@a)", c);
                        cmd.Parameters.AddWithValue("@t", text);
                        cmd.Parameters.AddWithValue("@a", ans);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    MessageBox.Show("Saved.", "Add", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                    mw.RefreshAndLoadQuestions();
                }
                catch (System.Exception ex)
                {
                    Logger.LogError(ex);
                    MessageBox.Show("DB error: " + ex.Message, "Add", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
