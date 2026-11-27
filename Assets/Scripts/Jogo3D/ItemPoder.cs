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
        static GameObject modelo;

        public static GameObject Criar(PowerUp p, Transform pai, float escala)
        {
            if (p == PowerUp.Nenhum) return null;
            if (modelo == null) modelo = Resources.Load<GameObject>("Jogo/itens");
            if (modelo == null) return null;

            GameObject go = Object.Instantiate(modelo, pai);
            go.name = "Item " + p;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * escala;
            string nome = "Item_" + (int)p;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Item_")) t.gameObject.SetActive(t.name == nome || t.name.StartsWith(nome + "_"));
            VisualRetro.Converter(go);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }
    }
}
