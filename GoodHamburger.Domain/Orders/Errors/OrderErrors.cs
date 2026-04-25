namespace GoodHamburger.Domain.Orders.Errors;

public static class OrderErrors
{
    public const string EmptyOrder = "Pedido não pode ser criado sem itens.";
    public const string DuplicateCategory = "Já existe um item da categoria '{0}' neste pedido.";
}
