using Storix_BE.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Interfaces
{
    public interface IWarehouseAssignmentRepository
    {
        Task<Warehouse?> GetWarehouseByIdAsync(int warehouseId);
        Task<List<Warehouse>> GetWarehousesByCompanyIdAsync(int companyId);
        Task<WarehouseAssignment?> GetAssignmentAsync(int userId, int warehouseId);
        Task<List<WarehouseAssignment>> GetAssignmentsByCompanyIdAsync(int companyId);
        Task<List<WarehouseAssignment>> GetAssignmentsByWarehouseIdAsync(int warehouseId);
        Task<int> CountAssignmentsByWarehouseIdAsync(int warehouseId);
        Task<int> CountAssignmentsByUserIdAsync(int userId);
        Task<bool> HasActiveWarehouseOperationsAsync(int userId, int warehouseId);
        Task<int> UpdateRoleInAssignmentsAsync(int userId, string roleInWarehouse);
        Task<int> AddAssignmentAsync(WarehouseAssignment assignment);
        Task<bool> RemoveAssignmentAsync(WarehouseAssignment assignment);
        Task<Warehouse> CreateWarehouseAsync(Warehouse warehouse);

        //I need a method to create a new warehouse, the json body will be like this:
        /*                                    
            {
              "width": 1200,
              "height": 800,
              "zones": [
                {
                  "id": "z-1771945437309",
                  "code": "Zone 1",
                  "x": 4.552142735600331,
                  "y": 4.7948802662302965,
                  "width": 329.7806830231329,
                  "height": 368.54558796187376,
                  "shelves": [
                    {
                      "id": "s-1771945440908",
                      "code": "S-1",
                      "x": 68.27356999999998,
                      "y": 36.71561,
                      "width": 40,
                      "height": 100,
                      "accessNodes": [
                        {
                          "id": "acc-top-1771945446825",
                          "side": "top",
                          "x": 88.27356999999998,
                          "y": 26.715609999999998
                        },
                        {
                          "id": "acc-bottom-1771945447330",
                          "side": "bottom",
                          "x": 88.27356999999998,
                          "y": 146.71561
                        },
                        {
                          "id": "acc-left-1771945447901",
                          "side": "left",
                          "x": 58.27356999999998,
                          "y": 86.71561
                        },
                        {
                          "id": "acc-right-1771945448683",
                          "side": "right",
                          "x": 118.27356999999998,
                          "y": 86.71561
                        }
                      ],
                      "levels": [
                        {
                          "id": "lvl-1771945679907",
                          "code": "L-1",
                          "bins": [
                            {
                              "id": "bin-1771945683631-0.8081820228199444",
                              "code": "B-1"
                            },
                            {
                              "id": "bin-1771945684407-0.4161617982786775",
                              "code": "B-2"
                            },
                            {
                              "id": "bin-1771945684685-0.5073059163791037",
                              "code": "B-3"
                            }
                          ]
                        },
                        {
                          "id": "lvl-1771945682346",
                          "code": "L-2",
                          "bins": [
                            {
                              "id": "bin-1771945685965-0.31038331362217575",
                              "code": "B-1"
                            },
                            {
                              "id": "bin-1771945686334-0.1643512713262686",
                              "code": "B-2"
                            },
                            {
                              "id": "bin-1771945686685-0.7434495456179145",
                              "code": "B-3"
                            }
                          ]
                        }
                      ]
                    },
                    {
                      "id": "s-1771945453865-0",
                      "code": "S-2",
                      "x": 185.00000000000003,
                      "y": 39,
                      "width": 40,
                      "height": 100,
                      "accessNodes": [
                        {
                          "id": "acc-1771945453865-0.034853462924109735",
                          "side": "top",
                          "x": 205.00000000000003,
                          "y": 29
                        },
                        {
                          "id": "acc-1771945453865-0.9239127882403402",
                          "side": "bottom",
                          "x": 205.00000000000003,
                          "y": 149
                        },
                        {
                          "id": "acc-1771945453865-0.9425099013985325",
                          "side": "left",
                          "x": 175.00000000000003,
                          "y": 89
                        },
                        {
                          "id": "acc-1771945453865-0.3688792457462936",
                          "side": "right",
                          "x": 235.00000000000003,
                          "y": 89
                        }
                      ],
                      "levels": []
                    },
                    {
                      "id": "s-1771945508620-0",
                      "code": "S-3",
                      "x": 69,
                      "y": 206,
                      "width": 40,
                      "height": 100,
                      "accessNodes": [
                        {
                          "id": "acc-1771945508620-0.4108789510516484",
                          "side": "top",
                          "x": 89,
                          "y": 196
                        },
                        {
                          "id": "acc-1771945508620-0.4962746255136925",
                          "side": "bottom",
                          "x": 89,
                          "y": 316
                        },
                        {
                          "id": "acc-1771945508620-0.5392497236313308",
                          "side": "left",
                          "x": 59,
                          "y": 256
                        },
                        {
                          "id": "acc-1771945508620-0.8183378304785868",
                          "side": "right",
                          "x": 119,
                          "y": 256
                        }
                      ],
                      "levels": [
                        {
                          "id": "lvl-1771945691471",
                          "code": "L-1",
                          "bins": [
                            {
                              "id": "bin-1771945692864-0.05414678050508026",
                              "code": "B-1"
                            }
                          ]
                        },
                        {
                          "id": "lvl-1771945691799",
                          "code": "L-2",
                          "bins": [
                            {
                              "id": "bin-1771945693987-0.3546264739789119",
                              "code": "B-1"
                            },
                            {
                              "id": "bin-1771945694121-0.4869033437470244",
                              "code": "B-2"
                            }
                          ]
                        },
                        {
                          "id": "lvl-1771945692183",
                          "code": "L-3",
                          "bins": [
                            {
                              "id": "bin-1771945695020-0.2910421301049687",
                              "code": "B-1"
                            },
                            {
                              "id": "bin-1771945695275-0.5278909521443125",
                              "code": "B-2"
                            },
                            {
                              "id": "bin-1771945695562-0.5933683689145852",
                              "code": "B-3"
                            }
                          ]
                        }
                      ]
                    },
                    {
                      "id": "s-1771945508620-1",
                      "code": "S-4",
                      "x": 185.72643000000005,
                      "y": 208.28439,
                      "width": 40,
                      "height": 100,
                      "accessNodes": [
                        {
                          "id": "acc-1771945508620-0.6992279828676002",
                          "side": "top",
                          "x": 205.72643000000005,
                          "y": 198.28439
                        },
                        {
                          "id": "acc-1771945508620-0.5271403438255811",
                          "side": "bottom",
                          "x": 205.72643000000005,
                          "y": 318.28439000000003
                        },
                        {
                          "id": "acc-1771945508620-0.2102703275847675",
                          "side": "left",
                          "x": 175.72643000000005,
                          "y": 258.28439000000003
                        },
                        {
                          "id": "acc-1771945508620-0.580993059230293",
                          "side": "right",
                          "x": 235.72643000000005,
                          "y": 258.28439000000003
                        }
                      ],
                      "levels": []
                    }
                  ]
                }
              ],
              "nodes": [
                {
                  "id": "n-1771945558855",
                  "x": 29.236489999999804,
                  "y": 16.39041999999995,
                  "radius": 8,
                },
                {
                  "id": "n-1771945559918",
                  "x": 153.2364899999998,
                  "y": 14.39041999999995,
                  "radius": 8,
                },
                {
                  "id": "n-1771945564361",
                  "x": 156.2364899999998,
                  "y": 181.39041999999995,
                  "radius": 8,
                },
                ...
              ],
              "edges": [
                {
                  "id": "e-1771945584806",
                  "from": "n-1771945558855",
                  "to": "n-1771945559918"
                },
                {
                  "id": "e-1771945585787",
                  "from": "n-1771945559918",
                  "to": "n-1771945567191"
                },
                {
                  "id": "e-1771945586796",
                  "from": "n-1771945567191",
                  "to": "n-1771945564361"
                },
                ...
              ]
            }
         */
    }
}
