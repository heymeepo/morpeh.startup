#if UNITY_EDITOR
using UnityEditor;

namespace Scellecs.Morpeh.Elysium
{
    [InitializeOnLoad]
    public class DefineSymbolHandler
    {
        private const string SYMBOL = "MORPEH_ELYSIUM";

        static DefineSymbolHandler()
        {
            if (IsDefineSymbolPresent(SYMBOL) == false)
            {
                AddDefineSymbol(SYMBOL);
            }
        }

        private static bool IsDefineSymbolPresent(string define)
        {
            string existingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            return existingDefines.Contains(define);
        }

        private static void AddDefineSymbol(string define)
        {
            string existingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, existingDefines + ";" + define);
        }
    }
}
#endif
