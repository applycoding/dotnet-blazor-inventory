namespace Stockroom.Application.Common;

internal static class PageRequest
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }
        else if (pageSize > 100)
        {
            pageSize = 100;
        }

        return (page, pageSize);
    }
}
