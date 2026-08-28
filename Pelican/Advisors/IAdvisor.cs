using Pelican.Model;

namespace Pelican.Advisors;

/// <summary>
/// Um conselheiro observa o estado e produz avisos. Nada mais.
///
/// Este e o unico ponto de extensao do mod: adicionar capacidade e adicionar
/// um IAdvisor e registra-lo. Nunca mexer no HUD para isso.
///
/// Contrato: Advise nao pode lancar. O ModEntry envolve tudo em try/catch de
/// qualquer forma, mas um advisor que quebra e um advisor que some da tela sem
/// avisar - prefira devolver lista vazia e logar.
/// </summary>
public interface IAdvisor
{
    /// <summary>Nome curto, usado no log de diagnostico e para ligar/desligar por config.</summary>
    string Name { get; }

    /// <summary>Le o estado e devolve os avisos. Pode devolver vazio.</summary>
    IEnumerable<Insight> Advise(GameSnapshot snapshot);
}
