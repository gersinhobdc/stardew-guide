namespace Pelican.Model;

/// <summary>
/// O que fechar uma sala do Centro Comunitario destrava.
/// Curado a mao: o jogo sabe as recompensas por bundle, mas nao expoe
/// "esta sala libera a Estufa" de forma consultavel.
/// Nomes casam por case-insensitive com as chaves do JSON (Newtonsoft, via helper.Data).
/// </summary>
public sealed class RoomReward
{
    public string Nome { get; set; } = "";
    public string Destrava { get; set; } = "";
    public string Detalhe { get; set; } = "";
}

/// <summary>Envelope do room-rewards.json. O envelope existe para o arquivo poder
/// carregar metadados sem quebrar a desserializacao do dicionario.</summary>
public sealed class RoomRewardFile
{
    public string Descricao { get; set; } = "";
    public Dictionary<string, RoomReward> Salas { get; set; } = new();
}
