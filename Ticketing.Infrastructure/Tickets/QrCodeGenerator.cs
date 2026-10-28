using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QRCoder;
using Ticketing.Application.Common.Interfaces;

namespace Ticketing.Infrastructure.Tickets;

public class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

        // PngByteQRCode ne dépend pas de System.Drawing : il fonctionne sur tous les systèmes
        var png = new PngByteQRCode(data);
        return png.GetGraphic(10);
    }
}
