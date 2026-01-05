import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-authentication',
  templateUrl: './authentication.component.html',
  styleUrls: ['./authentication.component.css']
})
export class AuthenticationComponent implements OnInit {
  isLoginMode: boolean = true;

  authData = {
    name: '',
    email: '',
    password: ''
  };
  errorMessage = '';

  constructor(private route: ActivatedRoute, private router: Router, private authService: AuthService) { }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.isLoginMode = params['mode'] === 'login';
      this.errorMessage = '';
    });
  }

  onSubmit() {
    if (this.isLoginMode) {
      this.authService.login({ email: this.authData.email, password: this.authData.password }).subscribe({
        next: () => this.router.navigate(['/']),
        error: (err) => this.errorMessage = "Identifiants incorrects."
      });
    } else {
      this.authService.register(this.authData).subscribe({
        next: () => this.router.navigate(['/auth/login']),
        error: (err) => this.errorMessage = "Erreur lors de l'inscription."
      });
    }
  }

  toggleMode() {
    const nextMode = this.isLoginMode ? 'register' : 'login';
    this.router.navigate(['/auth', nextMode]);
  }
}
