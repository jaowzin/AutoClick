using System.Windows.Forms;

namespace AutoClick;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Não foi possível iniciar o AutoClick.\n\n{exception.Message}",
                "AutoClick - Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
