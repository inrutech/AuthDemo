// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC20/utils/SafeERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/security/ReentrancyGuard.sol";
import "@openzeppelin/contracts/utils/Address.sol";


contract Official is Ownable, ReentrancyGuard {
    using SafeERC20 for IERC20;
    using Address for address payable;

    mapping(uint256 => bool) public processedBusinessIds;

    uint256 public constant MAX_REQUESTS = 1024;

    struct TransferRequest {
        address from;
        address to;
        address token;
        uint256 amount;
        uint256 businessId;
    }

    event ERC20BatchTransferPerformed(
        address indexed from,
        address indexed to,
        address indexed token,
        uint256 value,
        uint256 businessId
    );

    constructor() Ownable(msg.sender) {}

    function batchTransferToken(TransferRequest[] calldata requests)
        external
        onlyOwner
        nonReentrant
    {
        uint256 length = requests.length;
        require(
            length > 0 && length <= MAX_REQUESTS,
            "Invalid number of transactions"
        );

        for (uint256 i = 0; i < length; i++) {
            TransferRequest calldata request = requests[i];

            if (
                processedBusinessIds[request.businessId] ||
                request.from == request.to ||
                request.from == address(0) ||
                request.to == address(0) ||
                request.token == address(0) ||
                request.amount == 0
            ) {
                continue;
            }

            IERC20 token = IERC20(request.token);

            uint256 allowance = token.allowance(request.from, address(this));
            uint256 balance = token.balanceOf(request.from);

            if (allowance < request.amount || balance < request.amount) {
                continue;
            }

            processedBusinessIds[request.businessId] = true;

            token.safeTransferFrom(request.from, request.to, request.amount);

            emit ERC20BatchTransferPerformed(
                request.from,
                request.to,
                request.token,
                request.amount,
                request.businessId
            );
        }
    }
}