namespace WIA_ViewerProgram
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // ì¤‘ë³µ ?¤í–‰ ë°©ì?: ?„ì—­ Mutexë¡??¨ì¼ ?¸ìŠ¤?´ìŠ¤ë§??ˆìš©
            // - Global\ ?‘ë‘?¬ëŠ” ?°ë????œë¹„???¤ì¤‘ ?¸ì…˜ ?˜ê²½?ì„œ???™ì¼ ?¸ìŠ¤?´ìŠ¤ë¡??¸ì‹?œí‚¤ê¸??„í•¨
            // - ?„ìš” ?????Œì‚¬ëª…ìœ¼ë¡?ê³ ìœ ?˜ê²Œ ë°”ê¿”???©ë‹ˆ??
            using var mutex = new System.Threading.Mutex(
                initiallyOwned: true,
                name: @"Global\WIA_ViewerProgram_SingleInstance",
                createdNew: out var createdNew);

            if (!createdNew)
            {
                MessageBox.Show(
                    "?„ë¡œê·¸ë¨???´ë? ?¤í–‰ ì¤‘ì…?ˆë‹¤.",
                    "ì¤‘ë³µ ?¤í–‰ ë°©ì?",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // ?¼ë? ?°í???ì¡°í•©?ì„œ ApplicationConfiguration.Initialize()ê°€
            // ScaleHelper ?€??ì´ˆê¸°???ˆì™¸ë¥?? ë°œ?????ˆì–´ ?¸í™˜ ì´ˆê¸°?”ë¡œ ?€ì²´í•©?ˆë‹¤.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ViewerForm());
        }
    }
}