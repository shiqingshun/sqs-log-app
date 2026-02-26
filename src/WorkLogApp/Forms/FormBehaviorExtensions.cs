namespace WorkLogApp.Forms;

internal static class FormBehaviorExtensions
{
    public static void EnableEscClose(this Form form)
    {
        form.KeyPreview = true;
        form.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Escape)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            form.Close();
        };
    }
}
