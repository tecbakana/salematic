using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Salematic.Application.DTOs;
using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;

namespace Salematic.Application.Services
{
    public class ClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogger<ClienteService> _logger;
        private readonly IConfiguration _configuration;

        public ClienteService(IClienteRepository clienteRepository, ILogger<ClienteService> logger, IConfiguration configuration)
        {
            _clienteRepository = clienteRepository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ClienteModel> ProcessarAsync(CriarClienteRequest clienteDto)
        {
            try
            {
                var cliente = new ClienteModel
                {
                    Nome = clienteDto.Nome,
                    Documento = clienteDto.Documento,
                    Email = clienteDto.Email,
                    Telefone = clienteDto.Telefone,
                    Cep = clienteDto.Cep,
                    Logradouro = clienteDto.Logradouro,
                    Numero = clienteDto.Numero,
                    Complemento = clienteDto.Complemento,
                    Bairro = clienteDto.Bairro,
                    Cidade = clienteDto.Cidade,
                    Estado = clienteDto.Estado
                };
                return await _clienteRepository.ProcessarAsync(cliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar cliente: {ClienteData}", JsonSerializer.Serialize(clienteDto));
                throw;
            }
        }

        public async Task<ClienteModel?> ObterPorIdAsync(int id)
        {
            try
            {
                return await _clienteRepository.BuscarPorIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter cliente por ID: {ClienteId}", id);
                throw;
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existente = await _clienteRepository.BuscarPorEmailAsync(request.Email);
            if (existente != null)
                throw new InvalidOperationException("E-mail já cadastrado.");

            var cliente = new ClienteModel
            {
                Nome = request.Nome,
                Documento = request.Documento,
                Email = request.Email,
                Telefone = request.Telefone,
                Cep = request.Cep,
                Logradouro = request.Logradouro,
                Numero = request.Numero,
                Complemento = request.Complemento,
                Bairro = request.Bairro,
                Cidade = request.Cidade,
                Estado = request.Estado,
                AplicacaoId = request.AplicacaoId,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha)
            };

            var criado = await _clienteRepository.ProcessarAsync(cliente);
            return new AuthResponse
            {
                Token = GerarToken(criado),
                ClienteId = criado.SalematicClienteId,
                Nome = criado.Nome,
                Email = criado.Email
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var cliente = await _clienteRepository.BuscarPorEmailAsync(request.Email);
            if (cliente == null || string.IsNullOrEmpty(cliente.SenhaHash))
                throw new UnauthorizedAccessException("Credenciais inválidas.");

            if (!BCrypt.Net.BCrypt.Verify(request.Senha, cliente.SenhaHash))
                throw new UnauthorizedAccessException("Credenciais inválidas.");

            return new AuthResponse
            {
                Token = GerarToken(cliente),
                ClienteId = cliente.SalematicClienteId,
                Nome = cliente.Nome,
                Email = cliente.Email
            };
        }

        private string GerarToken(ClienteModel cliente)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key não configurado.");
            var issuer = _configuration["Jwt:Issuer"] ?? "Salematic";
            var audience = _configuration["Jwt:Audience"] ?? "SalematicClientes";
            var expHours = int.TryParse(_configuration["Jwt:ExpirationHours"], out var h) ? h : 24;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, cliente.SalematicClienteId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, cliente.Email),
                new Claim("nome", cliente.Nome),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(issuer, audience, claims,
                expires: DateTime.UtcNow.AddHours(expHours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
