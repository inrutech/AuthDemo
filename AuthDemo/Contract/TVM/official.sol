// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;


/**
 * @title SafeERC20 (简化版本)
 */
interface IERC20 {
    function transferFrom(address from, address to, uint256 amount) external returns (bool);

    function allowance(address owner, address spender) external view returns (uint256);

    function balanceOf(address account) external view returns (uint256);
}

library SafeERC20 {
    function safeTransferFrom(IERC20 token, address from, address to, uint256 value) internal {
        require(address(token) != address(0), "Invalid token");
        bool success = token.transferFrom(from, to, value);
        require(success, "Transfer failed");
    }
}

/**
 * @title Ownable (简化版)
 */
abstract contract Ownable {
    address private _owner;

    event OwnershipTransferred(address indexed previousOwner, address indexed newOwner);

    constructor(address initOwner) {
        require(initOwner != address(0), "Owner cannot be zero");
        _owner = initOwner;
        emit OwnershipTransferred(address(0), initOwner);
    }

    modifier onlyOwner() {
        require(msg.sender == _owner, "Not owner");
        _;
    }

    function owner() public view returns (address) {
        return _owner;
    }

    function transferOwnership(address newOwner) external onlyOwner {
        require(newOwner != address(0), "Invalid owner");
        emit OwnershipTransferred(_owner, newOwner);
        _owner = newOwner;
    }
}

/**
 * @title ReentrancyGuard (简化版)
 */
abstract contract ReentrancyGuard {
    uint256 private _status;
    constructor() {
        _status = 1;
    }
    modifier nonReentrant() {
        require(_status == 1, "Reentrancy detected");
        _status = 2;
        _;
        _status = 1;
    }
}

/**
 * @title Official 合约 (TVM 兼容版)
 */
contract Official is Ownable, ReentrancyGuard {
    using SafeERC20 for IERC20;

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