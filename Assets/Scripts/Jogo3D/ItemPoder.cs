using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// O objeto 3D de cada poder: uma coisa de verdade (banana, cantil, alicate, estilingue, coco, outra dinamite,
    /// tesoura, saco de terra, cola) amarrada numa banana de dinamite sozinha. E o que o macaco do fundo joga para
    /// a roda e o que fica no chao, do lado direito de quem guardou.
    ///
    /// Os nove estao no mesmo arquivo, Resources/Jogo/itens.glb, cada um num grupo "Item_N" (N = numero do poder no
    /// enum PowerUp). Sao modelados na previa (funcao ITENS) e exportados por __cena.exportaItens().
    /// </summary>
    public static class ItemPoder
    {
        /// <summary>Tudo = objeto amarrado na dinamite (o que o macaco do fundo joga). SoObjeto = ja desamarrado (guardado, em uso). SoBombinha = a dinamite que sobra.</summary>
        public enum Parte { Tudo, SoObjeto, SoBombinha }

        const float AlturaDaDinamite = 0.8f; // na medida do modelo: o objeto fica apoiado em cima dela

        static GameObject modelo;

        public static GameObject Criar(PowerUp p, Transform pai, float escala, Parte parte = Parte.Tudo)
        {
            if (p == PowerUp.Nenhum) return null;
            if (modelo == null) modelo = Resources.Load<GameObject>("Jogo/itens");
            if (modelo == null) return null;

            GameObject go = Object.Instantiate(modelo, pai);
            go.name = "Item " + p + (parte == Parte.SoBombinha ? " (bombinha)" : "");
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * escala;
            string nome = "Item_" + (int)p;
            Transform item = null;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Item_")) continue;
                bool este = t.name == nome || t.name.StartsWith(nome + "_");
                t.gameObject.SetActive(este);
                if (este) item = t;
            }
            if (item != null && parte != Parte.Tudo)
            {
                bool achou = false;
                foreach (Transform filho in item)
                {
                    bool bombinha = filho.name.StartsWith("Bombinha");
                    achou |= bombinha;
                    filho.gameObject.SetActive(parte == Parte.SoBombinha ? bombinha : !bombinha);
                }
                if (parte == Parte.SoBombinha && !achou) { Object.Destroy(go); return null; } // o maco inteiro nao tem bombinha
                if (parte == Parte.SoObjeto && achou) item.localPosition -= Vector3.up * AlturaDaDinamite; // desce para o chao
            }
            VisualRetro.Converter(go);
            return go;
        }
    }
}
