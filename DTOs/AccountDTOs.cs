using System;
using System.Collections.Generic;

namespace LightSwitchApplication.DTOs
{

	public class UserDTO
	{
		public string UserName { get; set; }
		public string Email { get; set; }
		public string FullName { get; set; }
		public string Password { get; set; }
		public bool IsLockedOut { get; set; }
		public bool IsOnline { get; set; }

	}

	public class ExpandedUserDTO
	{
		public string UserName { get; set; }
		public string FullName { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public DateTime CreationDate { get; set; }
		public bool IsOnline { get; set; }
		public bool IsLockedOut { get; set; }
		public DateTime LastLoginDate { get; set; }
		public DateTime LastPasswordChangeDate { get; set; }
		public IEnumerable<UserRolesDTO> Roles { get; set; }

	}

	public class UserRolesDTO
	{
		public string RoleName { get; set; }
		public IEnumerable<PermissionDTO> Permissions { get; set; }

	}

	public class RolePermissionDTO
	{
		public string RoleName { get; set; }
		public string PermissionId { get; set; }
	}

	public class UserRoleDTO
	{
		public string UserName { get; set; }
		public string RoleName { get; set; }
	}

	public class PermissionDTO
	{
		public string Id { get; set; }
		public string Name { get; set; }

	}

	public class ChangePasswordDTO
	{
		public string UserName { get; set; }
		public string OldPassword { get; set; }
		public string NewPassword { get; set; }
		public string ConfirmPassword { get; set; }
	}

	public class LoginDTO
	{
		public string UserName { get; set; }
		public string Password { get; set; }
		public bool Persistent { get; set; }
	}

	public class RoleDTO
	{
		public string RoleName { get; set; }
	}

	public class RoleUserDTO
	{
		public string RoleName { get; set; }
		public string UserName { get; set; }
	}

}