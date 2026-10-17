using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

namespace JazzCreateGames.BubbleFont.Free
{
    public class ScriptTextTest : MonoBehaviour
    {
        public TMP_Text bubbleText;
        public TMP_Text bubbleTitleTxt;
        public GameObject bubble;
        public GameObject bubbleTitle;

        void Start()
        {
            bubbleTitleTxt.text = "Font JazzCreate Bubble";

            bubbleText.text =
                "☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼▬↨↑↓→←\n" +
                "∟↔▲▼!#$%&'()*+,-./0123456789:;<=>?@\n" +
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ\n" +
                "`abcdefghijklmnopqrstuvwxyz\n" +
                "îìÄÅÉæÆôöòûùÿÖÜ \n" +
                "¢£¥ƒáíóúñÑªº¿¬½¼¡«»░▒▓\n" +
                "│┤╣║╗┐└┴┬├─┼╚╔╩╦╣║╗•§©®¶_\n" +
                "Thanks for using fonts from JazzCreatesGames 2015–2025 ©.";
        }
    }
}
