using Storix_BE.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Interfaces
{
    public interface IInventoryInboundRepository
    {
        Task<int> CreateInventoryInboundTicketRequest(InboundRequest request);
        Task<int> UpdateInventoryInboundTicketRequestStatus(int ticketRequestId, int approverId, string status);
    }
}
